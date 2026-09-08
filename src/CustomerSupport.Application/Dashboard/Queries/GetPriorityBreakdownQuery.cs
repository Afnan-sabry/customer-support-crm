using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetPriorityBreakdownQuery : IRequest<List<PriorityBreakdownDto>>;

public class GetPriorityBreakdownQueryHandler : IRequestHandler<GetPriorityBreakdownQuery, List<PriorityBreakdownDto>>
{
    private readonly AppDbContext _context;

    public GetPriorityBreakdownQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<PriorityBreakdownDto>> Handle(GetPriorityBreakdownQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        return await _context.Tickets
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => new { t.PriorityId, t.Priority!.Name, t.Priority!.NameAr, t.Priority!.Level })
            .Select(g => new PriorityBreakdownDto(g.Key.PriorityId, g.Key.Name, g.Key.NameAr, g.Key.Level, g.Count()))
            .OrderByDescending(p => p.Level)
            .ToListAsync(cancellationToken);
    }
}
