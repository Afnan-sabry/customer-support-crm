using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Escalation.Commands;

public record CreateEscalationRuleCommand(
    string Name, string NameAr,
    Guid? PriorityId, Guid? CategoryId,
    string TriggerType, int TriggerAfterMinutes,
    string ActionType, string? ActionTarget,
    int Order) : IRequest<EscalationRuleDto>;

public class CreateEscalationRuleCommandHandler : IRequestHandler<CreateEscalationRuleCommand, EscalationRuleDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateEscalationRuleCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<EscalationRuleDto> Handle(CreateEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new EscalationRule
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            PriorityId = request.PriorityId,
            CategoryId = request.CategoryId,
            TriggerType = request.TriggerType,
            TriggerAfterMinutes = request.TriggerAfterMinutes,
            ActionType = request.ActionType,
            ActionTarget = request.ActionTarget,
            Order = request.Order
        };

        await _context.EscalationRules.AddAsync(rule, cancellationToken);
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
