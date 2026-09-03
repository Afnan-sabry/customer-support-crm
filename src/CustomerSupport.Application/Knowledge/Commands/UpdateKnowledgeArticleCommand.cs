using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record UpdateKnowledgeArticleCommand(
    Guid Id, string Title, string TitleAr,
    string Content, string ContentAr,
    Guid CategoryId, string? Tags,
    bool IsPublished) : IRequest<KnowledgeArticleDetailDto>;

public class UpdateKnowledgeArticleCommandHandler : IRequestHandler<UpdateKnowledgeArticleCommand, KnowledgeArticleDetailDto>
{
    private readonly IKnowledgeRepository _repository;

    public UpdateKnowledgeArticleCommandHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<KnowledgeArticleDetailDto> Handle(UpdateKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _repository.GetArticleByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge article not found.");

        article.Title = request.Title;
        article.TitleAr = request.TitleAr;
        article.Content = request.Content;
        article.ContentAr = request.ContentAr;
        article.CategoryId = request.CategoryId;
        article.Tags = request.Tags;
        article.IsPublished = request.IsPublished;

        await _repository.UpdateArticleAsync(article, cancellationToken);

        return new KnowledgeArticleDetailDto(article.Id, article.Title, article.TitleAr,
            article.Content, article.ContentAr, article.CategoryId, article.Category?.Name ?? "",
            article.Tags, article.IsPublished, article.ViewCount, article.CreatedAt, article.UpdatedAt);
    }
}
