using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetAgentPerformanceReportQuery(
    DateTime StartDate, DateTime EndDate,
    Guid? AgentId = null) : IRequest<AgentPerformanceReportDto>;

public class GetAgentPerformanceReportQueryHandler : IRequestHandler<GetAgentPerformanceReportQuery, AgentPerformanceReportDto>
{
    private readonly AppDbContext _context;

    public GetAgentPerformanceReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<AgentPerformanceReportDto> Handle(GetAgentPerformanceReportQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var ticketQuery = _context.Tickets
            .Where(t => t.AssignedToId.HasValue
                && t.CreatedAt >= request.StartDate && t.CreatedAt <= request.EndDate);

        if (request.AgentId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.AssignedToId == request.AgentId.Value);

        var ticketData = await ticketQuery.Select(t => new
        {
            t.AssignedToId,
            AgentName = t.AssignedTo!.FullName,
            t.StatusId,
            t.CreatedAt,
            t.UpdatedAt
        }).ToListAsync(cancellationToken);

        var slaData = await _context.TicketSlas
            .Where(ts => ts.CreatedAt >= request.StartDate && ts.CreatedAt <= request.EndDate)
            .Select(ts => new
            {
                ts.Ticket.AssignedToId,
                ts.FirstResponseBreached,
                ts.ResolutionBreached,
                ts.FirstRespondedAt,
                ts.CreatedAt
            }).ToListAsync(cancellationToken);

        var agents = ticketData
            .GroupBy(t => new { t.AssignedToId, t.AgentName })
            .Select(g =>
            {
                var agentSla = slaData.Where(s => s.AssignedToId == g.Key.AssignedToId).ToList();
                var slaTotal = agentSla.Count;
                var slaOnTime = agentSla.Count(s => !s.FirstResponseBreached && !s.ResolutionBreached);
                var resolved = g.Where(t => finalStatusIds.Contains(t.StatusId)).ToList();
                var avgResolution = resolved.Count > 0
                    ? resolved.Average(t => (t.UpdatedAt - t.CreatedAt).TotalMinutes) : 0;
                var avgFirstResponse = agentSla.Where(s => s.FirstRespondedAt.HasValue).ToList();
                var avgFr = avgFirstResponse.Count > 0
                    ? avgFirstResponse.Average(s => (s.FirstRespondedAt!.Value - s.CreatedAt).TotalMinutes) : 0;

                return new AgentPerformanceItem(
                    g.Key.AssignedToId!.Value, g.Key.AgentName,
                    g.Count(), resolved.Count,
                    Math.Round(avgResolution, 1), Math.Round(avgFr, 1),
                    slaTotal > 0 ? Math.Round(slaOnTime * 100.0 / slaTotal, 1) : 100);
            })
            .OrderByDescending(a => a.SlaCompliancePercent)
            .ToList();

        var top = agents.FirstOrDefault();

        return new AgentPerformanceReportDto(
            agents,
            top is not null ? new AgentPerformanceTopPerformer(top.AgentId, top.AgentName) : null);
    }
}
