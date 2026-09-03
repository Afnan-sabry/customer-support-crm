using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetSlaPerformanceReportQuery(
    DateTime StartDate, DateTime EndDate,
    Guid? PriorityId = null, Guid? CategoryId = null) : IRequest<SlaPerformanceReportDto>;

public class GetSlaPerformanceReportQueryHandler : IRequestHandler<GetSlaPerformanceReportQuery, SlaPerformanceReportDto>
{
    private readonly AppDbContext _context;

    public GetSlaPerformanceReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<SlaPerformanceReportDto> Handle(GetSlaPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var slaQuery = _context.TicketSlas
            .Include(ts => ts.Ticket)
            .Where(ts => ts.CreatedAt >= request.StartDate && ts.CreatedAt <= request.EndDate);

        if (request.PriorityId.HasValue)
            slaQuery = slaQuery.Where(ts => ts.Ticket.PriorityId == request.PriorityId.Value);
        if (request.CategoryId.HasValue)
            slaQuery = slaQuery.Where(ts => ts.Ticket.CategoryId == request.CategoryId.Value);

        var slaRecords = await slaQuery.Select(ts => new
        {
            ts.CreatedAt,
            ts.FirstResponseBreached,
            ts.ResolutionBreached,
            ts.FirstRespondedAt,
            ts.ResolvedAt
        }).ToListAsync(cancellationToken);

        var frResolved = slaRecords.Where(s => s.FirstRespondedAt.HasValue).ToList();
        var resResolved = slaRecords.Where(s => s.ResolvedAt.HasValue).ToList();

        var frOnTime = frResolved.Count(s => !s.FirstResponseBreached);
        var frBreached = frResolved.Count(s => s.FirstResponseBreached);
        var resOnTime = resResolved.Count(s => !s.ResolutionBreached);
        var resBreached = resResolved.Count(s => s.ResolutionBreached);

        var frTotal = frOnTime + frBreached;
        var resTotal = resOnTime + resBreached;

        var timeSeries = slaRecords
            .GroupBy(s => s.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new SlaTimeSeriesPoint(
                g.Key,
                g.Count(s => s.FirstRespondedAt.HasValue && !s.FirstResponseBreached),
                g.Count(s => s.FirstResponseBreached),
                g.Count(s => s.ResolvedAt.HasValue && !s.ResolutionBreached),
                g.Count(s => s.ResolutionBreached)))
            .ToList();

        var breachDetails = await _context.SlaBreachLogs
            .Include(b => b.Ticket)
            .Include(b => b.SlaPolicy)
            .Where(b => b.CreatedAt >= request.StartDate && b.CreatedAt <= request.EndDate)
            .OrderByDescending(b => b.BreachedAt)
            .Take(50)
            .Select(b => new SlaBreachDetailItem(
                b.TicketId, b.Ticket.TicketNumber, b.BreachType,
                b.SlaPolicy.Name, b.DueAt, b.BreachedAt,
                (b.BreachedAt - b.DueAt).TotalMinutes))
            .ToListAsync(cancellationToken);

        return new SlaPerformanceReportDto(
            frTotal > 0 ? Math.Round(frOnTime * 100.0 / frTotal, 1) : 100,
            resTotal > 0 ? Math.Round(resOnTime * 100.0 / resTotal, 1) : 100,
            timeSeries, breachDetails);
    }
}
