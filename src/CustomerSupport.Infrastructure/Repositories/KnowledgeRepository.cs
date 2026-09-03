using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly AppDbContext _context;

    public KnowledgeRepository(AppDbContext context) => _context = context;

    public IQueryable<KnowledgeArticle> GetArticlesQueryable() => _context.KnowledgeArticles.AsQueryable();
    public IQueryable<KnowledgeCategory> GetCategoriesQueryable() => _context.KnowledgeCategories.AsQueryable();

    public async Task<KnowledgeArticle?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.KnowledgeArticles.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<KnowledgeArticle> AddArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeArticles.AddAsync(article, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return article;
    }

    public async Task UpdateArticleAsync(KnowledgeArticle article, CancellationToken cancellationToken = default)
    {
        _context.KnowledgeArticles.Update(article);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeCategory> AddCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default)
    {
        await _context.KnowledgeCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task UpdateCategoryAsync(KnowledgeCategory category, CancellationToken cancellationToken = default)
    {
        _context.KnowledgeCategories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
