using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Commands;

public record CreateAssignmentRuleCommand(
    string Name, string NameAr,
    Guid? CategoryId, Guid? PriorityId,
    string Strategy, string? AgentPool,
    int Order) : IRequest<AssignmentRuleDto>;

public class CreateAssignmentRuleCommandHandler : IRequestHandler<CreateAssignmentRuleCommand, AssignmentRuleDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateAssignmentRuleCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AssignmentRuleDto> Handle(CreateAssignmentRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new AssignmentRule
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            NameAr = request.NameAr,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            Strategy = request.Strategy,
            AgentPool = request.AgentPool,
            Order = request.Order
        };

        await _context.AssignmentRules.AddAsync(rule, cancellationToken);
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
