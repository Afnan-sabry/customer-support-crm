namespace CustomerSupport.Domain.Interfaces;

public record ChatbotResponse(
    string Content,
    bool ShouldEscalate,
    string? EscalationReason);

public interface IAiChatbotService
{
    Task<ChatbotResponse> GenerateResponseAsync(
        Guid conversationId, string userMessage, Guid tenantId,
        CancellationToken ct = default);
}
