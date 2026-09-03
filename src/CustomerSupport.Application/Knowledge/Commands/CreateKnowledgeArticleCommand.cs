using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Knowledge.Commands;

public record CreateKnowledgeArticleCommand(
    string Title, string TitleAr,
    string Content, string ContentAr,
    Guid CategoryId, string? Tags,
    bool IsPublished) : IRequest<KnowledgeArticleDto>;

public class CreateKnowledgeArticleCommandHandler : IRequestHandler<CreateKnowledgeArticleCommand, KnowledgeArticleDto>
{
    private readonly IKnowledgeRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppDbContext _context;

    public CreateKnowledgeArticleCommandHandler(IKnowledgeRepository repository, ICurrentUserService currentUserService, AppDbContext context)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<KnowledgeArticleDto> Handle(CreateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = new KnowledgeArticle
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Title = request.Title,
            TitleAr = request.TitleAr,
            Content = request.Content,
            ContentAr = request.ContentAr,
            CategoryId = request.CategoryId,
            Tags = request.Tags,
            IsPublished = request.IsPublished
        };

        await _repository.AddArticleAsync(article, cancellationToken);

        var categoryName = await _context.KnowledgeCategories
            .Where(c => c.Id == request.CategoryId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken) ?? "";

        return new KnowledgeArticleDto(article.Id, article.Title, article.TitleAr,
            article.CategoryId, categoryName, article.Tags, article.IsPublished, article.ViewCount, article.CreatedAt);
    }
}
