using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetCategoryDistributionQuery : IRequest<List<CategoryDistributionDto>>;

public class GetCategoryDistributionQueryHandler : IRequestHandler<GetCategoryDistributionQuery, List<CategoryDistributionDto>>
{
    private readonly AppDbContext _context;

    public GetCategoryDistributionQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<CategoryDistributionDto>> Handle(GetCategoryDistributionQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        return await _context.Tickets
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category!.NameAr })
            .Select(g => new CategoryDistributionDto(g.Key.CategoryId, g.Key.Name, g.Key.NameAr, g.Count()))
            .OrderByDescending(c => c.TicketCount)
            .ToListAsync(cancellationToken);
    }
}
