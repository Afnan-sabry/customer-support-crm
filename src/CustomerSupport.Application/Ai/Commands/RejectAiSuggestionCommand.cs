using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Ai.Commands;

public record RejectAiSuggestionCommand(Guid SuggestionId) : IRequest<Result>;

public class RejectAiSuggestionCommandHandler : IRequestHandler<RejectAiSuggestionCommand, Result>
{
    private readonly AppDbContext _context;

    public RejectAiSuggestionCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(RejectAiSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.AiSuggestions
            .FirstOrDefaultAsync(s => s.Id == request.SuggestionId, cancellationToken);

        if (suggestion is null) return Result.Failure(["Suggestion not found"]);
        if (suggestion.Status != "Pending") return Result.Failure(["Suggestion is not pending"]);

        suggestion.Status = "Rejected";
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
