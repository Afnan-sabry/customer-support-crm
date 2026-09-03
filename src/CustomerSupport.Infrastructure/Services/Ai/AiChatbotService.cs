using System.Text.Json;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class AiChatbotService : IAiChatbotService
{
    private readonly AppDbContext _context;
    private readonly IAiClient _aiClient;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly AiRateLimiter _rateLimiter;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly AiSettings _settings;
    private readonly ILogger<AiChatbotService> _logger;

    private static readonly string[] EscalationKeywords =
        ["agent", "human", "speak to someone", "talk to someone", "real person", "representative"];

    public AiChatbotService(
        AppDbContext context,
        IAiClient aiClient,
        IPromptTemplateService promptTemplateService,
        AiRateLimiter rateLimiter,
        IKnowledgeRepository knowledgeRepository,
        IOptions<AiSettings> settings,
        ILogger<AiChatbotService> logger)
    {
        _context = context;
        _aiClient = aiClient;
        _promptTemplateService = promptTemplateService;
        _rateLimiter = rateLimiter;
        _knowledgeRepository = knowledgeRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ChatbotResponse> GenerateResponseAsync(
        Guid conversationId, string userMessage, Guid tenantId, CancellationToken ct = default)
    {
        if (EscalationKeywords.Any(kw => userMessage.Contains(kw, StringComparison.OrdinalIgnoreCase)))
        {
            return new ChatbotResponse(
                "I understand you'd like to speak with an agent. Let me connect you now.",
                ShouldEscalate: true,
                EscalationReason: "Customer requested human agent");
        }

        var botTurnCount = await _context.Messages
            .Where(m => m.ConversationId == conversationId
                && m.SenderType == SenderType.System
                && m.Metadata != null && m.Metadata.Contains("aiBot"))
            .CountAsync(ct);

        if (botTurnCount >= _settings.ChatbotMaxTurns)
        {
            return new ChatbotResponse(
                "I've done my best to help. Let me connect you with a human agent for further assistance.",
                ShouldEscalate: true,
                EscalationReason: $"Max bot turns reached ({_settings.ChatbotMaxTurns})");
        }

        var searchTerms = userMessage.Length > 100 ? userMessage[..100] : userMessage;
        var searchLower = searchTerms.ToLower();

        var articles = await _knowledgeRepository.GetArticlesQueryable()
            .Where(a => a.IsActive && a.IsPublished)
            .Where(a => a.Title.ToLower().Contains(searchLower)
                     || a.Content.ToLower().Contains(searchLower)
                     || (a.Tags != null && a.Tags.ToLower().Contains(searchLower)))
            .Take(_settings.ChatbotKnowledgeArticleCount)
            .Select(a => new { a.Title, a.Content })
            .ToListAsync(ct);

        var knowledgeContext = articles.Count > 0
            ? string.Join("\n---\n", articles.Select(a =>
                $"Title: {a.Title}\n{(a.Content.Length > 500 ? a.Content[..500] + "..." : a.Content)}"))
            : "No relevant articles found.";

        var recentMessages = await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Take(10)
            .OrderBy(m => m.SentAt)
            .Select(m => new { m.SenderType, m.Content })
            .ToListAsync(ct);

        var conversationHistory = string.Join("\n", recentMessages.Select(m =>
            $"[{(m.SenderType == SenderType.Customer ? "Customer" : "Bot")}]: {m.Content}"));

        var prompt = _promptTemplateService.Render("chatbot.answer", new Dictionary<string, string>
        {
            ["question"] = userMessage,
            ["conversationHistory"] = conversationHistory,
            ["knowledgeArticles"] = knowledgeContext
        });

        string botResponseContent;
        try
        {
            await _rateLimiter.AcquireAsync(1000, ct);
            AiChatResponse response;
            var actualTokens = 0;
            try
            {
                response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
                actualTokens = response.PromptTokens + response.CompletionTokens;
            }
            finally
            {
                _rateLimiter.Release(1000, actualTokens);
            }
            botResponseContent = response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot AI call failed for conversation {ConversationId}", conversationId);
            return new ChatbotResponse(
                "I'm having trouble understanding right now. Let me connect you to an agent who can help.",
                ShouldEscalate: true,
                EscalationReason: "AI call failed");
        }

        var shouldEscalate = false;
        try
        {
            var checkPrompt = _promptTemplateService.Render("chatbot.escalation-check", new Dictionary<string, string>
            {
                ["question"] = userMessage,
                ["botResponse"] = botResponseContent
            });

            await _rateLimiter.AcquireAsync(200, ct);
            AiChatResponse checkResponse;
            var checkActualTokens = 0;
            try
            {
                checkResponse = await _aiClient.ChatAsync(new AiChatRequest(checkPrompt, [], MaxTokens: 100), ct);
                checkActualTokens = checkResponse.PromptTokens + checkResponse.CompletionTokens;
            }
            finally
            {
                _rateLimiter.Release(200, checkActualTokens);
            }

            using var doc = JsonDocument.Parse(checkResponse.Content);
            var root = doc.RootElement;
            if (root.TryGetProperty("confidence", out var confEl))
            {
                var confidence = confEl.GetDouble();
                if (confidence < _settings.ChatbotConfidenceThreshold)
                    shouldEscalate = true;
            }
            if (root.TryGetProperty("isGrounded", out var groundedEl) && !groundedEl.GetBoolean())
                shouldEscalate = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chatbot escalation check failed — continuing without escalation");
        }

        if (shouldEscalate)
        {
            return new ChatbotResponse(
                botResponseContent + "\n\nI'm not fully confident in this answer. Let me connect you with a human agent.",
                ShouldEscalate: true,
                EscalationReason: "Low confidence response");
        }

        return new ChatbotResponse(botResponseContent, ShouldEscalate: false, EscalationReason: null);
    }
}
