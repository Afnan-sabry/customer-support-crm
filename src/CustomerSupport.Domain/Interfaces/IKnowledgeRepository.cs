using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IKnowledgeRepository
{
    IQueryable<KnowledgeArticle> GetArticlesQueryable();
    IQueryable<KnowledgeCategory> GetCategoriesQueryable();
    Task<KnowledgeArticle?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KnowledgeArticle> AddArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default);
    Task UpdateArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default);
    Task<KnowledgeCategory> AddCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default);
}
