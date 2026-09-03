namespace CustomerSupport.Domain.Interfaces;

public record AiChatMessage(string Role, string Content);

public record AiChatRequest(
    string SystemPrompt,
    List<AiChatMessage> Messages,
    float Temperature = 0.3f,
    int MaxTokens = 1024);

public record AiChatResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    string Model);

public record AiEmbeddingResponse(
    float[] Embedding,
    int TokensUsed);

public interface IAiClient
{
    Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default);
    Task<AiEmbeddingResponse> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
