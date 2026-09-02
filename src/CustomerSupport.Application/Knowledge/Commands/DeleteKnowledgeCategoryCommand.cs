using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Knowledge.Commands;

public record DeleteKnowledgeCategoryCommand(Guid Id) : IRequest<Result>;

public class DeleteKnowledgeCategoryCommandHandler : IRequestHandler<DeleteKnowledgeCategoryCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteKnowledgeCategoryCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteKnowledgeCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.KnowledgeCategories.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Knowledge category not found.");

        category.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
