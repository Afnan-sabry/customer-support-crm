using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Queries;

public record GetKnowledgeCategoriesQuery(bool? IsActive) : IRequest<List<KnowledgeCategoryDto>>;

public class GetKnowledgeCategoriesQueryHandler : IRequestHandler<GetKnowledgeCategoriesQuery, List<KnowledgeCategoryDto>>
{
    private readonly IKnowledgeRepository _repository;

    public GetKnowledgeCategoriesQueryHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<List<KnowledgeCategoryDto>> Handle(GetKnowledgeCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetCategoriesQueryable().AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        return await query.OrderBy(c => c.Order)
            .Select(c => new KnowledgeCategoryDto(c.Id, c.Name, c.NameAr, c.ParentCategoryId, c.Order, c.IsActive))
            .ToListAsync(cancellationToken);
    }
}
