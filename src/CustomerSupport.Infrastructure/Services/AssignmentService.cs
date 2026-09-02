using System.Text.Json;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services;

public class AssignmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(AppDbContext context, ILogger<AssignmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid?> FindAssigneeAsync(Guid tenantId, Guid? categoryId, Guid? priorityId, CancellationToken cancellationToken)
    {
        var rules = await _context.AssignmentRules
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .Where(r => (r.CategoryId == null || r.CategoryId == categoryId)
                     && (r.PriorityId == null || r.PriorityId == priorityId))
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            var agentIds = GetAgentPool(rule);
            if (agentIds.Count == 0)
            {
                agentIds = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);
            }

            if (agentIds.Count == 0) continue;

            Guid? assigneeId = rule.Strategy switch
            {
                "RoundRobin" => RoundRobin(rule, agentIds),
                "LeastLoad" => await LeastLoadAsync(agentIds, cancellationToken),
                _ => null
            };

            if (assigneeId.HasValue)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Auto-assigned ticket to {UserId} via rule {RuleId} ({Strategy})",
                    assigneeId, rule.Id, rule.Strategy);
                return assigneeId;
            }
        }

        return null;
    }

    private static List<Guid> GetAgentPool(AssignmentRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.AgentPool)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(rule.AgentPool) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static Guid RoundRobin(AssignmentRule rule, List<Guid> agents)
    {
        var index = rule.LastAssignedIndex % agents.Count;
        rule.LastAssignedIndex = index + 1;
        return agents[index];
    }

    private async Task<Guid?> LeastLoadAsync(List<Guid> agents, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var workloads = await _context.Tickets
            .Where(t => t.AssignedToId.HasValue && agents.Contains(t.AssignedToId.Value))
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => t.AssignedToId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var allAgentLoads = agents.Select(a => new
        {
            AgentId = a,
            Count = workloads.FirstOrDefault(w => w.AgentId == a)?.Count ?? 0
        });

        return allAgentLoads.OrderBy(x => x.Count).First().AgentId;
    }
}
