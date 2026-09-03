using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
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
                using var doc = System.Text.Json.JsonDocument.Parse(suggestion.Output);
                var root = doc.RootElement;

                var ticket = await _context.Tickets.FindAsync([suggestion.TicketId], cancellationToken);
                if (ticket is not null)
                {
                    if (root.TryGetProperty("categoryId", out var catEl) && Guid.TryParse(catEl.GetString(), out var catId))
                    {
                        _context.Set<TicketHistory>().Add(new TicketHistory
                        {
                            Id = Guid.NewGuid(),
                            TicketId = ticket.Id,
                            Field = "CategoryId",
                            OldValue = ticket.CategoryId.ToString(),
                            NewValue = catId.ToString(),
                            CreatedAt = _dateTimeService.UtcNow
                        });
                        ticket.CategoryId = catId;
                    }

                    if (root.TryGetProperty("priorityId", out var priEl) && Guid.TryParse(priEl.GetString(), out var priId))
                    {
                        _context.Set<TicketHistory>().Add(new TicketHistory
                        {
                            Id = Guid.NewGuid(),
                            TicketId = ticket.Id,
                            Field = "PriorityId",
                            OldValue = ticket.PriorityId.ToString(),
                            NewValue = priId.ToString(),
                            CreatedAt = _dateTimeService.UtcNow
                        });
                        ticket.PriorityId = priId;
                    }
                }
            }
            catch (System.Text.Json.JsonException) { }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
