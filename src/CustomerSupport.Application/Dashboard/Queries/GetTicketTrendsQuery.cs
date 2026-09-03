using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetTicketTrendsQuery(int Days = 30) : IRequest<List<TicketTrendDto>>;

public class GetTicketTrendsQueryHandler : IRequestHandler<GetTicketTrendsQuery, List<TicketTrendDto>>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetTicketTrendsQueryHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<TicketTrendDto>> Handle(GetTicketTrendsQuery request, CancellationToken cancellationToken)
    {
        var since = _dateTimeService.UtcNow.Date.AddDays(-request.Days);
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var tickets = await _context.Tickets
            .Where(t => t.CreatedAt >= since)
            .Select(t => new { t.CreatedAt, t.StatusId, t.UpdatedAt })
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, request.Days + 1)
            .Select(i => since.AddDays(i))
            .Select(date => new TicketTrendDto(
                date.ToString("yyyy-MM-dd"),
                tickets.Count(t => t.CreatedAt.Date == date),
                tickets.Count(t => finalStatusIds.Contains(t.StatusId) && t.UpdatedAt.Date == date)))
            .ToList();
    }
}
