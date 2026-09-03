using CustomerSupport.Application.Ai.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Ai.Queries;

public record GetTicketSuggestionsQuery(Guid TicketId) : IRequest<List<AiSuggestionDto>>;

public class GetTicketSuggestionsQueryHandler : IRequestHandler<GetTicketSuggestionsQuery, List<AiSuggestionDto>>
{
    private readonly AppDbContext _context;

    public GetTicketSuggestionsQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<AiSuggestionDto>> Handle(GetTicketSuggestionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.AiSuggestions
            .Where(s => s.TicketId == request.TicketId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new AiSuggestionDto(
                s.Id, s.TicketId, s.Type,
                s.Output, s.Confidence,
                s.Status, s.AppliedAt,
                s.Model, s.TokensUsed,
                s.CreatedAt))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
