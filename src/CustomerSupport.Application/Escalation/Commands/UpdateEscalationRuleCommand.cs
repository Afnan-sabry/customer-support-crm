using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Escalation.Commands;

public record UpdateEscalationRuleCommand(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    string TriggerType, int TriggerAfterMinutes,
    string ActionType, string? ActionTarget,
    int Order) : IRequest<EscalationRuleDto>;

public class UpdateEscalationRuleCommandHandler : IRequestHandler<UpdateEscalationRuleCommand, EscalationRuleDto>
{
    private readonly AppDbContext _context;

    public UpdateEscalationRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<EscalationRuleDto> Handle(UpdateEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.EscalationRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Escalation rule not found.");

        rule.Name = request.Name;
        rule.NameAr = request.NameAr;
        rule.PriorityId = request.PriorityId;
        rule.CategoryId = request.CategoryId;
        rule.TriggerType = request.TriggerType;
        rule.TriggerAfterMinutes = request.TriggerAfterMinutes;
        rule.ActionType = request.ActionType;
        rule.ActionTarget = request.ActionTarget;
        rule.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);

        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new EscalationRuleDto(rule.Id, rule.Name, rule.NameAr,
            rule.PriorityId, priorityName, rule.CategoryId, categoryName,
            rule.TriggerType, rule.TriggerAfterMinutes, rule.ActionType, rule.ActionTarget,
            rule.Order, rule.IsActive);
    }
}
