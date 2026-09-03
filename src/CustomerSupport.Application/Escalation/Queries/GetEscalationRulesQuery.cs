using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Escalation.Queries;

public record GetEscalationRulesQuery(bool? IsActive) : IRequest<List<EscalationRuleDto>>;

public class GetEscalationRulesQueryHandler : IRequestHandler<GetEscalationRulesQuery, List<EscalationRuleDto>>
{
    private readonly AppDbContext _context;

    public GetEscalationRulesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<EscalationRuleDto>> Handle(GetEscalationRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.EscalationRules
            .Include(e => e.Priority)
            .Include(e => e.Category)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(e => e.IsActive == request.IsActive.Value);

        return await query.OrderBy(e => e.Order)
            .Select(e => new EscalationRuleDto(
                e.Id, e.Name, e.NameAr,
                e.PriorityId, e.Priority != null ? e.Priority.Name : null,
                e.CategoryId, e.Category != null ? e.Category.Name : null,
                e.TriggerType, e.TriggerAfterMinutes,
                e.ActionType, e.ActionTarget,
                e.Order, e.IsActive))
            .ToListAsync(cancellationToken);
    }
}
