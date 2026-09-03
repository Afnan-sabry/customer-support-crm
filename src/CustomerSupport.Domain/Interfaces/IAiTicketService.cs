namespace CustomerSupport.Domain.Interfaces;

public record AiCategorizationResult(
    Guid? SuggestedCategoryId, string? SuggestedCategoryName,
    Guid? SuggestedPriorityId, string? SuggestedPriorityName,
    decimal Confidence, bool AutoApplied, Guid SuggestionId);

public record AiSummaryResult(string Summary, Guid SuggestionId);

public record AiSuggestedRepliesResult(List<string> Suggestions);

public interface IAiTicketService
{
    Task<AiCategorizationResult> CategorizeAsync(Guid ticketId, CancellationToken ct = default);
    Task<AiSummaryResult> SummarizeAsync(Guid ticketId, CancellationToken ct = default);
    Task<AiSuggestedRepliesResult> SuggestRepliesAsync(Guid ticketId, CancellationToken ct = default);
}
