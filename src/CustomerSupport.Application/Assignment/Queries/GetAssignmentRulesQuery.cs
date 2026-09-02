using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Assignment.Queries;

public record GetAssignmentRulesQuery(bool? IsActive) : IRequest<List<AssignmentRuleDto>>;

public class GetAssignmentRulesQueryHandler : IRequestHandler<GetAssignmentRulesQuery, List<AssignmentRuleDto>>
{
    private readonly AppDbContext _context;

    public GetAssignmentRulesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<AssignmentRuleDto>> Handle(GetAssignmentRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AssignmentRules
            .Include(a => a.Category)
            .Include(a => a.Priority)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(a => a.IsActive == request.IsActive.Value);

        return await query.OrderBy(a => a.Order)
            .Select(a => new AssignmentRuleDto(
                a.Id, a.Name, a.NameAr,
                a.CategoryId, a.Category != null ? a.Category.Name : null,
                a.PriorityId, a.Priority != null ? a.Priority.Name : null,
                a.Strategy, a.AgentPool,
                a.Order, a.IsActive))
            .ToListAsync(cancellationToken);
    }
}
