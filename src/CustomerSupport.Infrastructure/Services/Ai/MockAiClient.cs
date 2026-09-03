using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class MockAiClient : IAiClient
{
    private readonly ILogger<MockAiClient> _logger;

    public MockAiClient(ILogger<MockAiClient> logger)
    {
        _logger = logger;
    }

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("[MockAI] Chat request — System: {System}, Messages: {Count}",
            request.SystemPrompt[..Math.Min(100, request.SystemPrompt.Length)],
            request.Messages.Count);

        await Task.Delay(500, ct);

        var response = GetMockResponse(request.SystemPrompt);

        _logger.LogDebug("[MockAI] Response: {Response}", response);

        return new AiChatResponse(
            Content: response,
            PromptTokens: request.SystemPrompt.Length / 4,
            CompletionTokens: response.Length / 4,
            Model: "mock-gpt-4");
    }

    public async Task<AiEmbeddingResponse> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        _logger.LogInformation("[MockAI] Embedding request — Text length: {Length}", text.Length);
        await Task.Delay(100, ct);

        var embedding = new float[1536];
        var random = new Random(text.GetHashCode());
        for (var i = 0; i < embedding.Length; i++)
            embedding[i] = (float)(random.NextDouble() * 2 - 1);

        return new AiEmbeddingResponse(embedding, text.Length / 4);
    }

    private static string GetMockResponse(string systemPrompt)
    {
        if (systemPrompt.Contains("classifier", StringComparison.OrdinalIgnoreCase))
        {
            return """{"categoryId": "00000000-0000-0000-0000-000000000000", "priorityId": "00000000-0000-0000-0000-000000000000", "confidence": 0.85}""";
        }

        if (systemPrompt.Contains("summarize", StringComparison.OrdinalIgnoreCase))
        {
            return "This ticket reports an issue that has been discussed in the comments. The customer initially described the problem, and the support team has been working to resolve it. A solution was proposed and is currently being verified.";
        }

        if (systemPrompt.Contains("suggest", StringComparison.OrdinalIgnoreCase))
        {
            return """["Thank you for reaching out. I've reviewed your concern and here's what I recommend...", "I understand the issue you're facing. Based on our documentation, you can resolve this by...", "I appreciate your patience. Let me help you with this — the next step would be to..."]""";
        }

        if (systemPrompt.Contains("chatbot", StringComparison.OrdinalIgnoreCase))
        {
            return "Based on our knowledge base, I can help you with that. Here's the relevant information...";
        }

        if (systemPrompt.Contains("grounded", StringComparison.OrdinalIgnoreCase))
        {
            return """{"isGrounded": true, "confidence": 0.8}""";
        }

        return "This is a mock AI response for development purposes.";
    }
}
