using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using CustomerSupport.Infrastructure.Services.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Ai.Commands;

public record AcceptAiSuggestionCommand(Guid SuggestionId) : IRequest<Result>;

public class AcceptAiSuggestionCommandHandler : IRequestHandler<AcceptAiSuggestionCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public AcceptAiSuggestionCommandHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(AcceptAiSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.AiSuggestions
            .FirstOrDefaultAsync(s => s.Id == request.SuggestionId, cancellationToken);

        if (suggestion is null) return Result.Failure(["Suggestion not found"]);
        if (suggestion.Status != "Pending") return Result.Failure(["Suggestion is not pending"]);

        suggestion.Status = "Accepted";
        suggestion.AppliedAt = _dateTimeService.UtcNow;

        if (suggestion.Type == "Categorization")
        {
            try
            {
                var ticket = await _context.Tickets.FindAsync([suggestion.TicketId], cancellationToken);
                if (ticket is not null)
                {
                    await CategorizationApplyHelper.ApplyCategorizationAsync(
                        suggestion.Output, ticket, _context, _dateTimeService, cancellationToken);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                suggestion.Status = "Pending";
                suggestion.AppliedAt = null;
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Failure(["Failed to parse AI suggestion output — suggestion was not applied"]);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
