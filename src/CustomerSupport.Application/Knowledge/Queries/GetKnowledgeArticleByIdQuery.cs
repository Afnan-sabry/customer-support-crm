using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Queries;

public record GetKnowledgeArticleByIdQuery(Guid Id) : IRequest<KnowledgeArticleDetailDto>;

public class GetKnowledgeArticleByIdQueryHandler : IRequestHandler<GetKnowledgeArticleByIdQuery, KnowledgeArticleDetailDto>
{
    private readonly IKnowledgeRepository _repository;
    private readonly AppDbContext _context;

    public GetKnowledgeArticleByIdQueryHandler(IKnowledgeRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<KnowledgeArticleDetailDto> Handle(GetKnowledgeArticleByIdQuery request, CancellationToken cancellationToken)
    {
        var article = await _repository.GetArticleByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge article not found.");

        article.ViewCount++;
        await _context.SaveChangesAsync(cancellationToken);

        return new KnowledgeArticleDetailDto(article.Id, article.Title, article.TitleAr,
            article.Content, article.ContentAr, article.CategoryId, article.Category?.Name ?? "",
            article.Tags, article.IsPublished, article.ViewCount, article.CreatedAt, article.UpdatedAt);
    }
}
