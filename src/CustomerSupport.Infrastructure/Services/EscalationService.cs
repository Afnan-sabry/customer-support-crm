using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services;

public class EscalationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(AppDbContext context, ILogger<EscalationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessBreachAsync(SlaBreachLog breach, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == breach.TicketId, cancellationToken);

        if (ticket == null) return;

        var triggerType = breach.BreachType switch
        {
            "FirstResponse" => "FirstResponseBreached",
            "Resolution" => "ResolutionBreached",
            _ => breach.BreachType
        };

        var rules = await _context.EscalationRules
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == breach.TenantId && r.IsActive)
            .Where(r => r.TriggerType == triggerType)
            .Where(r => (r.PriorityId == null || r.PriorityId == ticket.PriorityId)
                     && (r.CategoryId == null || r.CategoryId == ticket.CategoryId))
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            switch (rule.ActionType)
            {
                case "Reassign" when Guid.TryParse(rule.ActionTarget, out var targetUserId):
                    var oldAssignee = ticket.AssignedToId?.ToString();
                    ticket.AssignedToId = targetUserId;
                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticket.Id,
                        Field = "AssignedToId",
                        OldValue = oldAssignee,
                        NewValue = targetUserId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("Escalation: Ticket {TicketId} reassigned to {UserId} by rule {RuleId}",
                        ticket.Id, targetUserId, rule.Id);
                    break;

                case "ChangePriority" when Guid.TryParse(rule.ActionTarget, out var targetPriorityId):
                    var oldPriority = ticket.PriorityId.ToString();
                    ticket.PriorityId = targetPriorityId;
                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticket.Id,
                        Field = "PriorityId",
                        OldValue = oldPriority,
                        NewValue = targetPriorityId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("Escalation: Ticket {TicketId} priority changed to {PriorityId} by rule {RuleId}",
                        ticket.Id, targetPriorityId, rule.Id);
                    break;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
