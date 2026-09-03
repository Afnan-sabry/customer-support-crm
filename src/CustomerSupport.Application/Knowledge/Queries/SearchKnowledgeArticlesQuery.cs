using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Queries;

public record SearchKnowledgeArticlesQuery(string Query, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedList<KnowledgeArticleDto>>;

public class SearchKnowledgeArticlesQueryHandler : IRequestHandler<SearchKnowledgeArticlesQuery, PaginatedList<KnowledgeArticleDto>>
{
    private readonly IKnowledgeRepository _repository;

    public SearchKnowledgeArticlesQueryHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<PaginatedList<KnowledgeArticleDto>> Handle(SearchKnowledgeArticlesQuery request, CancellationToken cancellationToken)
    {
        var search = request.Query.ToLower();

        var query = _repository.GetArticlesQueryable()
            .Include(a => a.Category)
            .Where(a => a.IsActive && a.IsPublished)
            .Where(a => a.Title.ToLower().Contains(search)
                     || a.TitleAr.Contains(search)
                     || a.Content.ToLower().Contains(search)
                     || a.ContentAr.Contains(search)
                     || (a.Tags != null && a.Tags.ToLower().Contains(search)))
            .AsNoTracking()
            .OrderByDescending(a => a.ViewCount);

        var projected = query.Select(a => new KnowledgeArticleDto(
            a.Id, a.Title, a.TitleAr,
            a.CategoryId, a.Category.Name,
            a.Tags, a.IsPublished, a.ViewCount, a.CreatedAt));

        return await PaginatedList<KnowledgeArticleDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
