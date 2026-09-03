using System.ClientModel;
using Azure.AI.OpenAI;
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class AzureOpenAiClient : IAiClient
{
    private readonly AzureOpenAIClient _client;
    private readonly AiSettings _settings;
    private readonly ILogger<AzureOpenAiClient> _logger;

    public AzureOpenAiClient(IOptions<AiSettings> settings, ILogger<AzureOpenAiClient> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _client = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new ApiKeyCredential(_settings.ApiKey));
    }

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default)
    {
        var chatClient = _client.GetChatClient(_settings.ChatDeployment);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(request.SystemPrompt)
        };

        foreach (var msg in request.Messages)
        {
            messages.Add(msg.Role.ToLower() switch
            {
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                _ => new UserChatMessage(msg.Content)
            });
        }

        var options = new ChatCompletionOptions
        {
            Temperature = request.Temperature,
            MaxOutputTokenCount = request.MaxTokens
        };

        _logger.LogDebug("Azure OpenAI chat request — Model: {Model}, Messages: {Count}",
            _settings.ChatDeployment, messages.Count);

        var response = await chatClient.CompleteChatAsync(messages, options, ct);
        var completion = response.Value;

        _logger.LogDebug("Azure OpenAI chat response — Tokens: {Prompt}/{Completion}",
            completion.Usage.InputTokenCount, completion.Usage.OutputTokenCount);

        return new AiChatResponse(
            Content: completion.Content[0].Text,
            PromptTokens: completion.Usage.InputTokenCount,
            CompletionTokens: completion.Usage.OutputTokenCount,
            Model: _settings.ChatDeployment);
    }

    public async Task<AiEmbeddingResponse> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var embeddingClient = _client.GetEmbeddingClient(_settings.EmbeddingDeployment);

        var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
        var embedding = response.Value;

        return new AiEmbeddingResponse(
            Embedding: embedding.ToFloats().ToArray(),
            TokensUsed: text.Length / 4);
    }
}
