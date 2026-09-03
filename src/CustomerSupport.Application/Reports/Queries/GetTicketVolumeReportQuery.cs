using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetTicketVolumeReportQuery(
    DateTime StartDate, DateTime EndDate, string GroupBy = "Day",
    Guid? CategoryId = null, Guid? PriorityId = null,
    Guid? StatusId = null, Guid? AssignedToId = null) : IRequest<TicketVolumeReportDto>;

public class GetTicketVolumeReportQueryHandler : IRequestHandler<GetTicketVolumeReportQuery, TicketVolumeReportDto>
{
    private readonly AppDbContext _context;

    public GetTicketVolumeReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<TicketVolumeReportDto> Handle(GetTicketVolumeReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tickets
            .Where(t => t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate);

        if (request.CategoryId.HasValue) query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        if (request.PriorityId.HasValue) query = query.Where(t => t.PriorityId == request.PriorityId.Value);
        if (request.StatusId.HasValue) query = query.Where(t => t.StatusId == request.StatusId.Value);
        if (request.AssignedToId.HasValue) query = query.Where(t => t.AssignedToId == request.AssignedToId.Value);

        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var tickets = await query.Select(t => new
        {
            t.CreatedAt,
            t.StatusId,
            t.CategoryId,
            CategoryName = t.Category!.Name,
            CategoryNameAr = t.Category!.NameAr,
            t.PriorityId,
            PriorityName = t.Priority!.Name,
            PriorityNameAr = t.Priority!.NameAr
        }).ToListAsync(cancellationToken);

        var timeSeries = tickets
            .GroupBy(t => FormatPeriod(t.CreatedAt, request.GroupBy))
            .OrderBy(g => g.Key)
            .Select(g => new TimeSeriesPoint(
                g.Key,
                g.Count(),
                g.Count(t => finalStatusIds.Contains(t.StatusId))))
            .ToList();

        var categoryBreakdown = tickets
            .GroupBy(t => new { t.CategoryName, t.CategoryNameAr })
            .Select(g => new CategoryBreakdownItem(g.Key.CategoryName, g.Key.CategoryNameAr, g.Count()))
            .OrderByDescending(c => c.Count)
            .ToList();

        var priorityBreakdown = tickets
            .GroupBy(t => new { t.PriorityName, t.PriorityNameAr })
            .Select(g => new PriorityBreakdownItem(g.Key.PriorityName, g.Key.PriorityNameAr, g.Count()))
            .OrderByDescending(p => p.Count)
            .ToList();

        return new TicketVolumeReportDto(
            timeSeries, categoryBreakdown, priorityBreakdown,
            tickets.Count,
            tickets.Count(t => finalStatusIds.Contains(t.StatusId)));
    }

    private static string FormatPeriod(DateTime date, string groupBy) => groupBy switch
    {
        "Week" => $"{date.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(date):D2}",
        "Month" => date.ToString("yyyy-MM"),
        _ => date.ToString("yyyy-MM-dd")
    };
}
