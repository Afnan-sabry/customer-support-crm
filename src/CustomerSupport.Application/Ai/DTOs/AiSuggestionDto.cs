namespace CustomerSupport.Application.Ai.DTOs;

public record AiSuggestionDto(
    Guid Id, Guid TicketId, string Type,
    string Output, decimal? Confidence,
    string Status, DateTime? AppliedAt,
    string Model, int TokensUsed,
    DateTime CreatedAt);
