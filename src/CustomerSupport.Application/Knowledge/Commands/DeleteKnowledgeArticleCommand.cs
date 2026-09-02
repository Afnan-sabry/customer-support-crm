using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record DeleteKnowledgeArticleCommand(Guid Id) : IRequest<Result>;

public class DeleteKnowledgeArticleCommandHandler : IRequestHandler<DeleteKnowledgeArticleCommand, Result>
{
    private readonly IKnowledgeRepository _repository;

    public DeleteKnowledgeArticleCommandHandler(IKnowledgeRepository repository) => _repository = repository;

    public async Task<Result> Handle(DeleteKnowledgeArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _repository.GetArticleByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge article not found.");

        article.IsActive = false;
        await _repository.UpdateArticleAsync(article, cancellationToken);
        return Result.Success();
    }
}
