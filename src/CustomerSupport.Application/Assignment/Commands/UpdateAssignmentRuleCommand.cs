using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Commands;

public record UpdateAssignmentRuleCommand(
    Guid Id, string Name, string NameAr,
    Guid? CategoryId, Guid? PriorityId,
    string Strategy, string? AgentPool,
    int Order) : IRequest<AssignmentRuleDto>;

public class UpdateAssignmentRuleCommandHandler : IRequestHandler<UpdateAssignmentRuleCommand, AssignmentRuleDto>
{
    private readonly AppDbContext _context;

    public UpdateAssignmentRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<AssignmentRuleDto> Handle(UpdateAssignmentRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AssignmentRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Assignment rule not found.");

        rule.Name = request.Name;
        rule.NameAr = request.NameAr;
        rule.CategoryId = request.CategoryId;
        rule.PriorityId = request.PriorityId;
        rule.Strategy = request.Strategy;
        rule.AgentPool = request.AgentPool;
        rule.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);

        var categoryName = request.CategoryId.HasValue
            ? await _context.TicketCategories.Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var priorityName = request.PriorityId.HasValue
            ? await _context.TicketPriorities.Where(p => p.Id == request.PriorityId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new AssignmentRuleDto(rule.Id, rule.Name, rule.NameAr,
            rule.CategoryId, categoryName, rule.PriorityId, priorityName,
            rule.Strategy, rule.AgentPool, rule.Order, rule.IsActive);
    }
}
