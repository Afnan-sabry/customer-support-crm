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

        var breachedTicketIds = await _context.TicketSlas
            .Where(ts => ts.FirstResponseBreached || ts.ResolutionBreached)
            .Select(ts => ts.TicketId)
            .ToListAsync(cancellationToken);

        return await _context.Tickets
            .Where(t => t.AssignedToId.HasValue && !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => new { t.AssignedToId, t.AssignedTo!.FullName })
            .Select(g => new AgentWorkloadDto(
                g.Key.AssignedToId!.Value,
                g.Key.FullName,
                g.Count(),
                g.Count(t => breachedTicketIds.Contains(t.Id))))
            .OrderByDescending(a => a.OpenTickets)
            .ToListAsync(cancellationToken);
    }
}
