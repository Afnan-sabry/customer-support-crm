using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Queries;

public record GetKnowledgeArticlesQuery(
    Guid? CategoryId, bool? IsPublished,
    int Page = 1, int PageSize = 20) : IRequest<PaginatedList<KnowledgeArticleDto>>;

public class GetKnowledgeArticlesQueryHandler : IRequestHandler<GetKnowledgeArticlesQuery, PaginatedList<KnowledgeArticleDto>>
{
    private readonly IKnowledgeRepository _repository;

    public GetKnowledgeArticlesQueryHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<PaginatedList<KnowledgeArticleDto>> Handle(GetKnowledgeArticlesQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetArticlesQueryable()
            .Include(a => a.Category)
            .Where(a => a.IsActive)
            .AsNoTracking();

        if (request.CategoryId.HasValue)
            query = query.Where(a => a.CategoryId == request.CategoryId.Value);

        if (request.IsPublished.HasValue)
            query = query.Where(a => a.IsPublished == request.IsPublished.Value);

        query = query.OrderByDescending(a => a.CreatedAt);

        var projected = query.Select(a => new KnowledgeArticleDto(
            a.Id, a.Title, a.TitleAr,
            a.CategoryId, a.Category.Name,
            a.Tags, a.IsPublished, a.ViewCount, a.CreatedAt));

        return await PaginatedList<KnowledgeArticleDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
