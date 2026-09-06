using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetTeamWorkloadQuery : IRequest<List<AgentWorkloadDto>>;

public class GetTeamWorkloadQueryHandler : IRequestHandler<GetTeamWorkloadQuery, List<AgentWorkloadDto>>
{
    private readonly AppDbContext _context;

    public GetTeamWorkloadQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<AgentWorkloadDto>> Handle(GetTeamWorkloadQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var workload = await (
            from t in _context.Tickets
            where t.AssignedToId.HasValue && !finalStatusIds.Contains(t.StatusId)
            join u in _context.Users on t.AssignedToId equals u.Id
            group t by new { AgentId = u.Id, AgentName = u.FullName } into g
            orderby g.Count() descending
            select new { g.Key.AgentId, g.Key.AgentName, OpenTickets = g.Count() }
        ).ToListAsync(cancellationToken);

        var breachCounts = await _context.TicketSlas
            .Where(ts => (ts.FirstResponseBreached || ts.ResolutionBreached)
                && ts.Ticket.AssignedToId.HasValue
                && !finalStatusIds.Contains(ts.Ticket.StatusId))
            .GroupBy(ts => ts.Ticket.AssignedToId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.AgentId, g => g.Count, cancellationToken);

        return workload.Select(w => new AgentWorkloadDto(
            w.AgentId,
            w.AgentName,
            w.OpenTickets,
            breachCounts.GetValueOrDefault(w.AgentId)
        )).ToList();
    }
}
