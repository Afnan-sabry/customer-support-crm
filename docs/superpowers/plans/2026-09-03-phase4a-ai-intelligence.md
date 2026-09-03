# Phase 4A — AI Intelligence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add AI-powered intelligence to the CRM — a foundational AI service layer (Azure OpenAI with mock provider), AI ticket features (auto-categorization, summarization, suggested replies), and a customer-facing chatbot integrated into the portal live chat.

**Architecture:** Thin `IAiClient` abstraction over Azure OpenAI with swappable `MockAiClient` for development. `AiTicketService` facade handles all ticket AI operations (categorize, summarize, suggest replies), storing results in an `AiSuggestion` audit entity. `AiChatbotService` uses RAG (retrieve knowledge articles → augment prompt → generate response) with escalation to human agents when the bot can't help.

**Tech Stack:** .NET 10, EF Core 10, Azure.AI.OpenAI NuGet, MediatR, FluentValidation, Angular 20, Angular Material, ngx-translate

**Spec:** `docs/superpowers/specs/2026-09-03-phase4a-ai-intelligence-design.md`

## Global Constraints

- .NET 10 (`net10.0` TFM), Angular 20 (CLI 20.3.10)
- SQL Server via EF Core Code-First migrations
- Every business entity has `TenantId` (Guid) — enforced by EF Core global query filter
- Bilingual fields: paired `Name` + `NameAr` columns (or `Title` + `TitleAr`)
- API routes: `api/v1/{resource}`
- JWT Bearer auth on all endpoints; permission-based authorization via `[Authorize(Policy = "Permission:xxx")]`
- MediatR for all Application layer commands/queries
- FluentValidation for all command validation
- Angular standalone components with `inject()`, Angular Material, `@for`/`@if` control flow, translate pipe
- Angular services extend `ApiService` with typed methods using `get<T>`, `post<T>`, `put<T>`, `delete<T>`
- i18n keys in `en.json`/`ar.json` — all user-facing text translated
- Phase 4A entities extend earlier phases via foreign keys without modifying existing tables
- All AI calls are non-blocking — failures are caught and logged, never blocking core CRM operations
- Mock provider used in development; real Azure OpenAI provider swappable via `AiSettings.Provider` config

## Dependency Graph

```
T1 (AI Service Foundation) ──┬──> T2 (AI Ticket Features — Backend)
                              │     └──> T3 (AI Ticket Features — Frontend)
                              └──> T4 (AI Chatbot)
```

**Execution order:** T1 → T2 → T3 → T4

## File Map

### Backend — New Files by Task

**Task 1 — AI Service Foundation:**
```
src/CustomerSupport.Domain/
  Interfaces/IAiClient.cs
  Interfaces/IPromptTemplateService.cs
src/CustomerSupport.Infrastructure/
  Services/Ai/AiSettings.cs
  Services/Ai/AzureOpenAiClient.cs
  Services/Ai/MockAiClient.cs
  Services/Ai/AiRateLimiter.cs
  Services/Ai/PromptTemplateService.cs
  Services/Ai/PromptTemplates.cs
```

**Task 2 — AI Ticket Features (Backend):**
```
src/CustomerSupport.Domain/
  Entities/AiSuggestion.cs
  Interfaces/IAiTicketService.cs
src/CustomerSupport.Application/
  Ai/DTOs/AiSuggestionDto.cs
  Ai/Commands/CategorizeTicketCommand.cs
  Ai/Commands/SummarizeTicketCommand.cs
  Ai/Commands/SuggestReplyCommand.cs
  Ai/Commands/AcceptAiSuggestionCommand.cs
  Ai/Commands/RejectAiSuggestionCommand.cs
  Ai/Queries/GetTicketSuggestionsQuery.cs
  Ai/Validators/CategorizeTicketValidator.cs
  Ai/Validators/SummarizeTicketValidator.cs
  Ai/Validators/SuggestReplyValidator.cs
  Ai/Handlers/AiCategorizationHandler.cs
src/CustomerSupport.Infrastructure/
  Persistence/Configurations/AiSuggestionConfiguration.cs
  Services/Ai/AiTicketService.cs
src/CustomerSupport.API/
  Controllers/AiController.cs
```

**Task 3 — AI Ticket Features (Frontend):**
```
src/client/src/app/
  features/ai/ai.service.ts
  features/ai/ai-suggestion-panel/ai-suggestion-panel.ts
  features/ai/ai-summary/ai-summary.ts
  features/ai/ai-suggest-replies-dialog/ai-suggest-replies-dialog.ts
```

**Task 4 — AI Chatbot:**
```
src/CustomerSupport.Domain/
  Interfaces/IAiChatbotService.cs
src/CustomerSupport.Infrastructure/
  Services/Ai/AiChatbotService.cs
```

### Modified Files (across tasks)

```
src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj — add Azure.AI.OpenAI NuGet (T1)
src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs — add AiSuggestions DbSet (T2)
src/CustomerSupport.Infrastructure/DependencyInjection.cs — register AI services (T1, T2, T4)
src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs — add ai.use, ai.manage (T1)
src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs — add ai.use to Agent role (T1)
src/CustomerSupport.API/Program.cs — bind AiSettings configuration (T1)
src/CustomerSupport.API/appsettings.json — add AiSettings section (T1)
src/CustomerSupport.API/appsettings.Development.json — add AiSettings with Provider=Mock (T1)
src/CustomerSupport.API/Hubs/ChatHub.cs — add chatbot routing logic (T4)
src/CustomerSupport.Infrastructure/Services/ChatSessionService.cs — skip auto-assign when chatbot enabled (T4)
src/client/src/app/features/tickets/ticket-detail/ticket-detail.ts — embed AI components (T3)
src/client/src/assets/i18n/en.json — add AI and chatbot keys (T3, T4)
src/client/src/assets/i18n/ar.json — add Arabic translations (T3, T4)
```

---

### Task 1: AI Service Foundation

**Files:**
- Create: `src/CustomerSupport.Domain/Interfaces/IAiClient.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IPromptTemplateService.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/AiSettings.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/MockAiClient.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/AzureOpenAiClient.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/AiRateLimiter.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/PromptTemplates.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/PromptTemplateService.cs`
- Modify: `src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs`
- Modify: `src/CustomerSupport.API/Program.cs`
- Modify: `src/CustomerSupport.API/appsettings.json`
- Modify: `src/CustomerSupport.API/appsettings.Development.json`

**Interfaces:**
- Consumes: `IDateTimeService` (from Phase 1), `IConfiguration` / `IOptions<T>` (ASP.NET Core)
- Produces:
  - `IAiClient` interface + `AiChatRequest`, `AiChatMessage`, `AiChatResponse`, `AiEmbeddingResponse` records
  - `IPromptTemplateService` interface
  - `AiSettings` configuration class
  - `MockAiClient : IAiClient` implementation (used in development)
  - `AzureOpenAiClient : IAiClient` implementation (used in production)
  - `AiRateLimiter` singleton service
  - `PromptTemplateService : IPromptTemplateService` implementation
  - `PromptTemplates` static class with all template strings
  - Permissions: `ai.use`, `ai.manage`

- [ ] **Step 1: Add Azure.AI.OpenAI NuGet package**

Add the `Azure.AI.OpenAI` package to the Infrastructure project:

```xml
<!-- Add to src/CustomerSupport.Infrastructure/CustomerSupport.Infrastructure.csproj ItemGroup -->
<PackageReference Include="Azure.AI.OpenAI" Version="2.2.0" />
```

- [ ] **Step 2: Create IAiClient interface with records**

```csharp
// src/CustomerSupport.Domain/Interfaces/IAiClient.cs
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
```

- [ ] **Step 3: Create IPromptTemplateService interface**

```csharp
// src/CustomerSupport.Domain/Interfaces/IPromptTemplateService.cs
namespace CustomerSupport.Domain.Interfaces;

public interface IPromptTemplateService
{
    string GetTemplate(string key);
    string Render(string key, Dictionary<string, string> placeholders);
}
```

- [ ] **Step 4: Create AiSettings configuration class**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/AiSettings.cs
namespace CustomerSupport.Infrastructure.Services.Ai;

public class AiSettings
{
    public string Provider { get; set; } = "AzureOpenAI";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeployment { get; set; } = "gpt-4";
    public string EmbeddingDeployment { get; set; } = "text-embedding-ada-002";
    public int MaxConcurrentRequests { get; set; } = 10;
    public int MaxTokensPerMinute { get; set; } = 60000;
    public float DefaultTemperature { get; set; } = 0.3f;
    public int DefaultMaxTokens { get; set; } = 1024;
    public bool ChatbotEnabled { get; set; } = true;
    public int ChatbotMaxTurns { get; set; } = 5;
    public double ChatbotConfidenceThreshold { get; set; } = 0.6;
    public int ChatbotKnowledgeArticleCount { get; set; } = 5;
    public double CategorizationAutoApplyThreshold { get; set; } = 0.8;
}
```

- [ ] **Step 5: Create PromptTemplates static class**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/PromptTemplates.cs
namespace CustomerSupport.Infrastructure.Services.Ai;

public static class PromptTemplates
{
    public static readonly Dictionary<string, string> Templates = new()
    {
        ["ticket.categorize"] = """
            You are a support ticket classifier. Analyze the ticket and classify it into a category and priority.

            Available categories (id — name):
            {categories}

            Available priorities (id — name):
            {priorities}

            Ticket subject: {subject}
            Ticket description: {description}

            Respond with ONLY valid JSON, no markdown, no explanation:
            {"categoryId": "<guid>", "priorityId": "<guid>", "confidence": <0.0-1.0>}

            If you cannot confidently classify, use the most general category and set confidence below 0.5.
            """,

        ["ticket.summarize"] = """
            You are a support ticket summarizer. Create a concise summary of this ticket conversation.

            Ticket subject: {subject}
            Ticket description: {description}

            Comments:
            {comments}

            Write a clear, professional summary in 2-4 sentences covering: the issue, key updates, and current status.
            """,

        ["ticket.suggest-reply"] = """
            You are a customer support agent assistant. Suggest helpful replies for this ticket.

            Ticket subject: {subject}
            Ticket description: {description}

            Recent messages:
            {recentMessages}

            Relevant knowledge base articles:
            {knowledgeContext}

            Generate 1-3 distinct, professional reply options. Each reply should be helpful and address the customer's concern.
            Respond with ONLY valid JSON, no markdown, no explanation:
            ["reply option 1", "reply option 2", "reply option 3"]
            """,

        ["chatbot.answer"] = """
            You are a helpful customer support chatbot. Answer the customer's question using ONLY the provided knowledge base articles.

            Knowledge base articles:
            {knowledgeArticles}

            Conversation history:
            {conversationHistory}

            Customer's question: {question}

            Rules:
            - Only answer based on the provided knowledge base articles
            - If the articles don't contain relevant information, say you don't have enough information and offer to connect them with a human agent
            - Be concise, friendly, and professional
            - Do not make up information
            """,

        ["chatbot.escalation-check"] = """
            You are evaluating whether a chatbot response is grounded in the provided knowledge base.

            Customer question: {question}
            Bot response: {botResponse}

            Respond with ONLY valid JSON, no markdown:
            {"isGrounded": true/false, "confidence": <0.0-1.0>}

            Set isGrounded=false and low confidence if the response seems fabricated or not supported by knowledge base content.
            """
    };
}
```

- [ ] **Step 6: Create PromptTemplateService**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/PromptTemplateService.cs
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class PromptTemplateService : IPromptTemplateService
{
    public string GetTemplate(string key)
    {
        return PromptTemplates.Templates.TryGetValue(key, out var template)
            ? template
            : throw new KeyNotFoundException($"Prompt template '{key}' not found.");
    }

    public string Render(string key, Dictionary<string, string> placeholders)
    {
        var template = GetTemplate(key);
        foreach (var (placeholder, value) in placeholders)
        {
            template = template.Replace($"{{{placeholder}}}", value);
        }
        return template;
    }
}
```

- [ ] **Step 7: Create AiRateLimiter**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/AiRateLimiter.cs
using Microsoft.Extensions.Options;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class AiRateLimiter
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly int _maxTokensPerMinute;
    private readonly object _tokenLock = new();
    private int _tokensUsedThisWindow;
    private DateTime _windowStart;

    public AiRateLimiter(IOptions<AiSettings> settings)
    {
        _concurrencySemaphore = new SemaphoreSlim(settings.Value.MaxConcurrentRequests);
        _maxTokensPerMinute = settings.Value.MaxTokensPerMinute;
        _windowStart = DateTime.UtcNow;
    }

    public async Task AcquireAsync(int estimatedTokens, CancellationToken ct = default)
    {
        if (!await _concurrencySemaphore.WaitAsync(TimeSpan.FromSeconds(30), ct))
            throw new InvalidOperationException("AI rate limit exceeded: too many concurrent requests.");

        lock (_tokenLock)
        {
            if (DateTime.UtcNow - _windowStart > TimeSpan.FromMinutes(1))
            {
                _tokensUsedThisWindow = 0;
                _windowStart = DateTime.UtcNow;
            }

            if (_tokensUsedThisWindow + estimatedTokens > _maxTokensPerMinute)
            {
                _concurrencySemaphore.Release();
                throw new InvalidOperationException("AI rate limit exceeded: token budget exhausted for this minute.");
            }

            _tokensUsedThisWindow += estimatedTokens;
        }
    }

    public void Release(int actualTokens)
    {
        lock (_tokenLock)
        {
            var diff = actualTokens - 0;
            if (diff > 0)
                _tokensUsedThisWindow = Math.Max(0, _tokensUsedThisWindow + diff);
        }
        _concurrencySemaphore.Release();
    }
}
```

- [ ] **Step 8: Create MockAiClient**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/MockAiClient.cs
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
```

- [ ] **Step 9: Create AzureOpenAiClient**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/AzureOpenAiClient.cs
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
            TokensUsed: response.Value.ToString().Length / 4);
    }
}
```

- [ ] **Step 10: Add AiSettings to appsettings.json**

Add the `AiSettings` section to `src/CustomerSupport.API/appsettings.json` after the `"AllowedHosts"` line:

```json
"AiSettings": {
  "Provider": "AzureOpenAI",
  "Endpoint": "",
  "ApiKey": "",
  "ChatDeployment": "gpt-4",
  "EmbeddingDeployment": "text-embedding-ada-002",
  "MaxConcurrentRequests": 10,
  "MaxTokensPerMinute": 60000,
  "DefaultTemperature": 0.3,
  "DefaultMaxTokens": 1024,
  "ChatbotEnabled": true,
  "ChatbotMaxTurns": 5,
  "ChatbotConfidenceThreshold": 0.6,
  "ChatbotKnowledgeArticleCount": 5,
  "CategorizationAutoApplyThreshold": 0.8
}
```

- [ ] **Step 11: Add AiSettings override to appsettings.Development.json**

Add to `src/CustomerSupport.API/appsettings.Development.json`:

```json
"AiSettings": {
  "Provider": "Mock"
}
```

- [ ] **Step 12: Bind AiSettings in Program.cs**

Add to `src/CustomerSupport.API/Program.cs` after `builder.Services.AddInfrastructureServices(builder.Configuration);` (line 28):

```csharp
builder.Services.Configure<CustomerSupport.Infrastructure.Services.Ai.AiSettings>(
    builder.Configuration.GetSection("AiSettings"));
```

- [ ] **Step 13: Register AI services in DependencyInjection.cs**

Add to `src/CustomerSupport.Infrastructure/DependencyInjection.cs` — add usings at top:

```csharp
using CustomerSupport.Infrastructure.Services.Ai;
```

Add at the end of `AddInfrastructureServices` method, before `return services;`:

```csharp
// AI Services
services.AddSingleton<AiRateLimiter>();
services.AddScoped<IPromptTemplateService, PromptTemplateService>();

// Register AI client based on configuration
services.AddScoped<IAiClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config.GetValue<string>("AiSettings:Provider") ?? "AzureOpenAI";
    if (provider == "Mock")
        return ActivatorUtilities.CreateInstance<MockAiClient>(sp);
    return ActivatorUtilities.CreateInstance<AzureOpenAiClient>(sp);
});
```

Add `using Microsoft.Extensions.Configuration;` if not already present (it is already there at line 14).

- [ ] **Step 14: Add AI permissions to seeders**

Add to `src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs` `AllPermissions` array, after `("notifications.view", "Notifications", "View notifications")`:

```csharp
("ai.use", "AI", "Trigger AI features"),
("ai.manage", "AI", "Configure AI settings"),
```

Add `"ai.use"` to the `AgentPermissions` array in `src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs`:

```csharp
private static readonly string[] AgentPermissions =
[
    "tickets.view", "tickets.create", "tickets.edit", "tickets.assign",
    "customers.view", "knowledgebase.view", "dashboard.view", "assignment.view",
    "conversations.view", "conversations.manage", "chat.view", "notifications.view",
    "ai.use"
];
```

- [ ] **Step 15: Verify build**

Run:
```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
```
Expected: 0 errors.

- [ ] **Step 16: Commit**

```bash
git add -A
git commit -m "feat(ai): add AI service foundation with Azure OpenAI client, mock provider, prompt templates, and rate limiter"
```

---

### Task 2: AI Ticket Features (Backend)

**Files:**
- Create: `src/CustomerSupport.Domain/Entities/AiSuggestion.cs`
- Create: `src/CustomerSupport.Domain/Interfaces/IAiTicketService.cs`
- Create: `src/CustomerSupport.Infrastructure/Persistence/Configurations/AiSuggestionConfiguration.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/AiTicketService.cs`
- Create: `src/CustomerSupport.Application/Ai/DTOs/AiSuggestionDto.cs`
- Create: `src/CustomerSupport.Application/Ai/Commands/CategorizeTicketCommand.cs`
- Create: `src/CustomerSupport.Application/Ai/Commands/SummarizeTicketCommand.cs`
- Create: `src/CustomerSupport.Application/Ai/Commands/SuggestReplyCommand.cs`
- Create: `src/CustomerSupport.Application/Ai/Commands/AcceptAiSuggestionCommand.cs`
- Create: `src/CustomerSupport.Application/Ai/Commands/RejectAiSuggestionCommand.cs`
- Create: `src/CustomerSupport.Application/Ai/Queries/GetTicketSuggestionsQuery.cs`
- Create: `src/CustomerSupport.Application/Ai/Validators/CategorizeTicketValidator.cs`
- Create: `src/CustomerSupport.Application/Ai/Validators/SummarizeTicketValidator.cs`
- Create: `src/CustomerSupport.Application/Ai/Validators/SuggestReplyValidator.cs`
- Create: `src/CustomerSupport.Application/Ai/Handlers/AiCategorizationHandler.cs`
- Create: `src/CustomerSupport.API/Controllers/AiController.cs`
- Modify: `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IAiClient`, `IPromptTemplateService`, `AiRateLimiter`, `AiSettings` (from Task 1); `Ticket`, `TicketComment`, `TicketHistory`, `TicketCategory`, `TicketPriority`, `KnowledgeArticle` entities; `IKnowledgeRepository`, `ICurrentUserService`, `IDateTimeService`, `AppDbContext`; `TicketCreatedNotification` (from Phase 2)
- Produces:
  - `AiSuggestion` entity
  - `IAiTicketService` interface + `AiCategorizationResult`, `AiSummaryResult`, `AiSuggestedRepliesResult` records
  - `AiSuggestionDto` DTO
  - `AiTicketService : IAiTicketService` implementation
  - `AiCategorizationHandler : INotificationHandler<TicketCreatedNotification>` (auto-categorization on ticket creation)
  - MediatR commands: `CategorizeTicketCommand`, `SummarizeTicketCommand`, `SuggestReplyCommand`, `AcceptAiSuggestionCommand`, `RejectAiSuggestionCommand`
  - MediatR query: `GetTicketSuggestionsQuery`
  - `AiController` API at `api/v1/ai`

- [ ] **Step 1: Create AiSuggestion entity**

```csharp
// src/CustomerSupport.Domain/Entities/AiSuggestion.cs
namespace CustomerSupport.Domain.Entities;

public class AiSuggestion : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public decimal? Confidence { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? AppliedAt { get; set; }
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }

    public Ticket? Ticket { get; set; }
}
```

- [ ] **Step 2: Create IAiTicketService interface with result records**

```csharp
// src/CustomerSupport.Domain/Interfaces/IAiTicketService.cs
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
```

- [ ] **Step 3: Create AiSuggestion EF configuration**

```csharp
// src/CustomerSupport.Infrastructure/Persistence/Configurations/AiSuggestionConfiguration.cs
using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class AiSuggestionConfiguration : IEntityTypeConfiguration<AiSuggestion>
{
    public void Configure(EntityTypeBuilder<AiSuggestion> builder)
    {
        builder.ToTable("AiSuggestions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Input).IsRequired();
        builder.Property(a => a.Output).IsRequired();
        builder.Property(a => a.Confidence).HasPrecision(5, 4);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Model).IsRequired().HasMaxLength(100);

        builder.HasOne(a => a.Ticket).WithMany().HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TenantId, a.TicketId });
        builder.HasIndex(a => new { a.TenantId, a.Type, a.Status });
    }
}
```

- [ ] **Step 4: Add AiSuggestions DbSet to AppDbContext**

Add to `src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs` after the `Notifications` DbSet:

```csharp
public DbSet<AiSuggestion> AiSuggestions => Set<AiSuggestion>();
```

- [ ] **Step 5: Create AiSuggestionDto**

```csharp
// src/CustomerSupport.Application/Ai/DTOs/AiSuggestionDto.cs
namespace CustomerSupport.Application.Ai.DTOs;

public record AiSuggestionDto(
    Guid Id, Guid TicketId, string Type,
    string Output, decimal? Confidence,
    string Status, DateTime? AppliedAt,
    string Model, int TokensUsed,
    DateTime CreatedAt);
```

- [ ] **Step 6: Create AiTicketService implementation**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/AiTicketService.cs
using System.Text.Json;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class AiTicketService : IAiTicketService
{
    private readonly AppDbContext _context;
    private readonly IAiClient _aiClient;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly AiRateLimiter _rateLimiter;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly AiSettings _settings;
    private readonly ILogger<AiTicketService> _logger;

    public AiTicketService(
        AppDbContext context,
        IAiClient aiClient,
        IPromptTemplateService promptTemplateService,
        AiRateLimiter rateLimiter,
        IKnowledgeRepository knowledgeRepository,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IOptions<AiSettings> settings,
        ILogger<AiTicketService> logger)
    {
        _context = context;
        _aiClient = aiClient;
        _promptTemplateService = promptTemplateService;
        _rateLimiter = rateLimiter;
        _knowledgeRepository = knowledgeRepository;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiCategorizationResult> CategorizeAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found");

        var categories = await _context.TicketCategories
            .Where(c => c.IsActive)
            .Select(c => $"{c.Id} — {c.Name}")
            .ToListAsync(ct);

        var priorities = await _context.TicketPriorities
            .Where(p => p.IsActive)
            .Select(p => $"{p.Id} — {p.Name}")
            .ToListAsync(ct);

        var prompt = _promptTemplateService.Render("ticket.categorize", new Dictionary<string, string>
        {
            ["subject"] = ticket.Subject,
            ["description"] = ticket.Description.Length > 1500
                ? ticket.Description[..1500] + "..."
                : ticket.Description,
            ["categories"] = string.Join("\n", categories),
            ["priorities"] = string.Join("\n", priorities)
        });

        await _rateLimiter.AcquireAsync(500, ct);
        AiChatResponse response;
        try
        {
            response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
        }
        finally
        {
            _rateLimiter.Release(0);
        }

        Guid? categoryId = null;
        string? categoryName = null;
        Guid? priorityId = null;
        string? priorityName = null;
        decimal confidence = 0;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            if (root.TryGetProperty("categoryId", out var catEl) && Guid.TryParse(catEl.GetString(), out var parsedCat))
            {
                categoryId = parsedCat;
                categoryName = await _context.TicketCategories
                    .Where(c => c.Id == parsedCat).Select(c => c.Name).FirstOrDefaultAsync(ct);
            }

            if (root.TryGetProperty("priorityId", out var priEl) && Guid.TryParse(priEl.GetString(), out var parsedPri))
            {
                priorityId = parsedPri;
                priorityName = await _context.TicketPriorities
                    .Where(p => p.Id == parsedPri).Select(p => p.Name).FirstOrDefaultAsync(ct);
            }

            if (root.TryGetProperty("confidence", out var confEl))
                confidence = confEl.GetDecimal();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI categorization response for ticket {TicketId}", ticketId);
            confidence = 0;
        }

        var inputText = $"Subject: {ticket.Subject}\nDescription: {ticket.Description}";
        if (inputText.Length > 2000) inputText = inputText[..2000];

        var suggestion = new AiSuggestion
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            TicketId = ticketId,
            Type = "Categorization",
            Input = inputText,
            Output = response.Content,
            Confidence = confidence,
            Status = "Pending",
            Model = response.Model,
            TokensUsed = response.PromptTokens + response.CompletionTokens
        };

        var autoApplied = false;
        if ((double)confidence >= _settings.CategorizationAutoApplyThreshold && categoryId.HasValue)
        {
            suggestion.Status = "AutoApplied";
            suggestion.AppliedAt = _dateTimeService.UtcNow;
            autoApplied = true;

            var ticketToUpdate = await _context.Tickets.FindAsync([ticketId], ct);
            if (ticketToUpdate is not null)
            {
                if (categoryId.HasValue)
                {
                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticketId,
                        Field = "CategoryId",
                        OldValue = ticketToUpdate.CategoryId.ToString(),
                        NewValue = categoryId.Value.ToString(),
                        CreatedAt = _dateTimeService.UtcNow
                    });
                    ticketToUpdate.CategoryId = categoryId.Value;
                }

                if (priorityId.HasValue)
                {
                    _context.Set<TicketHistory>().Add(new TicketHistory
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticketId,
                        Field = "PriorityId",
                        OldValue = ticketToUpdate.PriorityId.ToString(),
                        NewValue = priorityId.Value.ToString(),
                        CreatedAt = _dateTimeService.UtcNow
                    });
                    ticketToUpdate.PriorityId = priorityId.Value;
                }
            }
        }

        await _context.AiSuggestions.AddAsync(suggestion, ct);
        await _context.SaveChangesAsync(ct);

        return new AiCategorizationResult(
            categoryId, categoryName, priorityId, priorityName,
            confidence, autoApplied, suggestion.Id);
    }

    public async Task<AiSummaryResult> SummarizeAsync(Guid ticketId, CancellationToken ct = default)
    {
        var existingSummary = await _context.AiSuggestions
            .Where(s => s.TicketId == ticketId && s.Type == "Summary")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existingSummary is not null)
        {
            var latestComment = await _context.TicketComments
                .Where(c => c.TicketId == ticketId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (latestComment <= existingSummary.CreatedAt)
                return new AiSummaryResult(existingSummary.Output, existingSummary.Id);
        }

        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found");

        var comments = await _context.TicketComments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .Include(c => c.User)
            .Take(50)
            .ToListAsync(ct);

        var commentText = string.Join("\n\n", comments.Select(c =>
            $"[{c.CreatedAt:g}] {c.User?.FullName ?? "System"}{(c.IsInternal ? " (internal)" : "")}: {c.Content}"));

        var prompt = _promptTemplateService.Render("ticket.summarize", new Dictionary<string, string>
        {
            ["subject"] = ticket.Subject,
            ["description"] = ticket.Description.Length > 1000
                ? ticket.Description[..1000] + "..."
                : ticket.Description,
            ["comments"] = commentText.Length > 3000
                ? commentText[..3000] + "..."
                : commentText
        });

        await _rateLimiter.AcquireAsync(1000, ct);
        AiChatResponse response;
        try
        {
            response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
        }
        finally
        {
            _rateLimiter.Release(0);
        }

        var inputText = $"Subject: {ticket.Subject}\nComments: {comments.Count}";
        if (inputText.Length > 2000) inputText = inputText[..2000];

        var suggestion = new AiSuggestion
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            TicketId = ticketId,
            Type = "Summary",
            Input = inputText,
            Output = response.Content,
            Confidence = null,
            Status = "AutoApplied",
            AppliedAt = _dateTimeService.UtcNow,
            Model = response.Model,
            TokensUsed = response.PromptTokens + response.CompletionTokens
        };

        await _context.AiSuggestions.AddAsync(suggestion, ct);
        await _context.SaveChangesAsync(ct);

        return new AiSummaryResult(response.Content, suggestion.Id);
    }

    public async Task<AiSuggestedRepliesResult> SuggestRepliesAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found");

        var recentComments = await _context.TicketComments
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(10)
            .Include(c => c.User)
            .ToListAsync(ct);

        var recentMessages = string.Join("\n", recentComments
            .OrderBy(c => c.CreatedAt)
            .Select(c => $"[{c.User?.FullName ?? "Customer"}]: {c.Content}"));

        var searchTerms = ticket.Subject;
        if (ticket.Category is not null)
            searchTerms += " " + ticket.Category.Name;

        var articles = await _knowledgeRepository.GetArticlesQueryable()
            .Where(a => a.IsActive && a.IsPublished)
            .Where(a => a.Title.Contains(searchTerms) || a.Content.Contains(searchTerms)
                || (a.Tags != null && a.Tags.Contains(searchTerms)))
            .Take(5)
            .Select(a => new { a.Title, a.Content })
            .ToListAsync(ct);

        var knowledgeContext = articles.Count > 0
            ? string.Join("\n---\n", articles.Select(a =>
                $"Title: {a.Title}\n{(a.Content.Length > 500 ? a.Content[..500] + "..." : a.Content)}"))
            : "No relevant knowledge base articles found.";

        var prompt = _promptTemplateService.Render("ticket.suggest-reply", new Dictionary<string, string>
        {
            ["subject"] = ticket.Subject,
            ["description"] = ticket.Description.Length > 500
                ? ticket.Description[..500] + "..."
                : ticket.Description,
            ["recentMessages"] = recentMessages.Length > 2000
                ? recentMessages[..2000] + "..."
                : recentMessages,
            ["knowledgeContext"] = knowledgeContext
        });

        await _rateLimiter.AcquireAsync(1500, ct);
        AiChatResponse response;
        try
        {
            response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
        }
        finally
        {
            _rateLimiter.Release(0);
        }

        var suggestions = new List<string>();
        try
        {
            suggestions = JsonSerializer.Deserialize<List<string>>(response.Content) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI reply suggestions for ticket {TicketId}", ticketId);
            suggestions = [response.Content];
        }

        return new AiSuggestedRepliesResult(suggestions);
    }
}
```

- [ ] **Step 7: Create MediatR commands**

```csharp
// src/CustomerSupport.Application/Ai/Commands/CategorizeTicketCommand.cs
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Ai.Commands;

public record CategorizeTicketCommand(Guid TicketId) : IRequest<AiCategorizationResult>;

public class CategorizeTicketCommandHandler : IRequestHandler<CategorizeTicketCommand, AiCategorizationResult>
{
    private readonly IAiTicketService _aiTicketService;

    public CategorizeTicketCommandHandler(IAiTicketService aiTicketService) => _aiTicketService = aiTicketService;

    public async Task<AiCategorizationResult> Handle(CategorizeTicketCommand request, CancellationToken cancellationToken)
        => await _aiTicketService.CategorizeAsync(request.TicketId, cancellationToken);
}
```

```csharp
// src/CustomerSupport.Application/Ai/Commands/SummarizeTicketCommand.cs
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Ai.Commands;

public record SummarizeTicketCommand(Guid TicketId) : IRequest<AiSummaryResult>;

public class SummarizeTicketCommandHandler : IRequestHandler<SummarizeTicketCommand, AiSummaryResult>
{
    private readonly IAiTicketService _aiTicketService;

    public SummarizeTicketCommandHandler(IAiTicketService aiTicketService) => _aiTicketService = aiTicketService;

    public async Task<AiSummaryResult> Handle(SummarizeTicketCommand request, CancellationToken cancellationToken)
        => await _aiTicketService.SummarizeAsync(request.TicketId, cancellationToken);
}
```

```csharp
// src/CustomerSupport.Application/Ai/Commands/SuggestReplyCommand.cs
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Ai.Commands;

public record SuggestReplyCommand(Guid TicketId) : IRequest<AiSuggestedRepliesResult>;

public class SuggestReplyCommandHandler : IRequestHandler<SuggestReplyCommand, AiSuggestedRepliesResult>
{
    private readonly IAiTicketService _aiTicketService;

    public SuggestReplyCommandHandler(IAiTicketService aiTicketService) => _aiTicketService = aiTicketService;

    public async Task<AiSuggestedRepliesResult> Handle(SuggestReplyCommand request, CancellationToken cancellationToken)
        => await _aiTicketService.SuggestRepliesAsync(request.TicketId, cancellationToken);
}
```

```csharp
// src/CustomerSupport.Application/Ai/Commands/AcceptAiSuggestionCommand.cs
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
```

```csharp
// src/CustomerSupport.Application/Ai/Commands/RejectAiSuggestionCommand.cs
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Ai.Commands;

public record RejectAiSuggestionCommand(Guid SuggestionId) : IRequest<Result>;

public class RejectAiSuggestionCommandHandler : IRequestHandler<RejectAiSuggestionCommand, Result>
{
    private readonly AppDbContext _context;

    public RejectAiSuggestionCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(RejectAiSuggestionCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _context.AiSuggestions
            .FirstOrDefaultAsync(s => s.Id == request.SuggestionId, cancellationToken);

        if (suggestion is null) return Result.Failure(["Suggestion not found"]);
        if (suggestion.Status != "Pending") return Result.Failure(["Suggestion is not pending"]);

        suggestion.Status = "Rejected";
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

- [ ] **Step 8: Create GetTicketSuggestionsQuery**

```csharp
// src/CustomerSupport.Application/Ai/Queries/GetTicketSuggestionsQuery.cs
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
```

- [ ] **Step 9: Create validators**

```csharp
// src/CustomerSupport.Application/Ai/Validators/CategorizeTicketValidator.cs
using CustomerSupport.Application.Ai.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Ai.Validators;

public class CategorizeTicketValidator : AbstractValidator<CategorizeTicketCommand>
{
    public CategorizeTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
```

```csharp
// src/CustomerSupport.Application/Ai/Validators/SummarizeTicketValidator.cs
using CustomerSupport.Application.Ai.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Ai.Validators;

public class SummarizeTicketValidator : AbstractValidator<SummarizeTicketCommand>
{
    public SummarizeTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
```

```csharp
// src/CustomerSupport.Application/Ai/Validators/SuggestReplyValidator.cs
using CustomerSupport.Application.Ai.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Ai.Validators;

public class SuggestReplyValidator : AbstractValidator<SuggestReplyCommand>
{
    public SuggestReplyValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
```

- [ ] **Step 10: Create AiCategorizationHandler (auto-trigger on ticket creation)**

```csharp
// src/CustomerSupport.Application/Ai/Handlers/AiCategorizationHandler.cs
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Ai.Handlers;

public class AiCategorizationHandler : MediatR.INotificationHandler<TicketCreatedNotification>
{
    private readonly IAiTicketService _aiTicketService;
    private readonly ILogger<AiCategorizationHandler> _logger;

    public AiCategorizationHandler(IAiTicketService aiTicketService, ILogger<AiCategorizationHandler> logger)
    {
        _aiTicketService = aiTicketService;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aiTicketService.CategorizeAsync(notification.TicketId, cancellationToken);
            _logger.LogInformation(
                "AI categorized ticket {TicketId}: Category={Category}, Priority={Priority}, Confidence={Confidence}, AutoApplied={AutoApplied}",
                notification.TicketId, result.SuggestedCategoryName, result.SuggestedPriorityName,
                result.Confidence, result.AutoApplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI categorization failed for ticket {TicketId} — skipping", notification.TicketId);
        }
    }
}
```

- [ ] **Step 11: Create AiController**

```csharp
// src/CustomerSupport.API/Controllers/AiController.cs
using CustomerSupport.Application.Ai.Commands;
using CustomerSupport.Application.Ai.DTOs;
using CustomerSupport.Application.Ai.Queries;
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiController(IMediator mediator) => _mediator = mediator;

    [HttpPost("tickets/{ticketId:guid}/categorize")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<AiCategorizationResult>> CategorizeTicket(Guid ticketId)
        => Ok(await _mediator.Send(new CategorizeTicketCommand(ticketId)));

    [HttpPost("tickets/{ticketId:guid}/summarize")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<AiSummaryResult>> SummarizeTicket(Guid ticketId)
        => Ok(await _mediator.Send(new SummarizeTicketCommand(ticketId)));

    [HttpPost("tickets/{ticketId:guid}/suggest-replies")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<AiSuggestedRepliesResult>> SuggestReplies(Guid ticketId)
        => Ok(await _mediator.Send(new SuggestReplyCommand(ticketId)));

    [HttpGet("tickets/{ticketId:guid}/suggestions")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<List<AiSuggestionDto>>> GetSuggestions(Guid ticketId)
        => Ok(await _mediator.Send(new GetTicketSuggestionsQuery(ticketId)));

    [HttpPut("suggestions/{suggestionId:guid}/accept")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<Result>> AcceptSuggestion(Guid suggestionId)
    {
        var result = await _mediator.Send(new AcceptAiSuggestionCommand(suggestionId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("suggestions/{suggestionId:guid}/reject")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<Result>> RejectSuggestion(Guid suggestionId)
    {
        var result = await _mediator.Send(new RejectAiSuggestionCommand(suggestionId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
```

- [ ] **Step 12: Register IAiTicketService in DependencyInjection.cs**

Add to `src/CustomerSupport.Infrastructure/DependencyInjection.cs` inside `AddInfrastructureServices`, after the AI client registration from Task 1:

```csharp
services.AddScoped<IAiTicketService, AiTicketService>();
```

- [ ] **Step 13: Create and apply EF migration**

Run:
```bash
dotnet ef migrations add AddAiSuggestion --project src/CustomerSupport.Infrastructure --startup-project src/CustomerSupport.API --output-dir Persistence/Migrations
```

- [ ] **Step 14: Verify build**

Run:
```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
```
Expected: 0 errors.

- [ ] **Step 15: Commit**

```bash
git add -A
git commit -m "feat(ai): add AI ticket features — categorization, summarization, suggested replies with AiSuggestion audit trail"
```

---

### Task 3: AI Ticket Features (Frontend)

**Files:**
- Create: `src/client/src/app/features/ai/ai.service.ts`
- Create: `src/client/src/app/features/ai/ai-suggestion-panel/ai-suggestion-panel.ts`
- Create: `src/client/src/app/features/ai/ai-summary/ai-summary.ts`
- Create: `src/client/src/app/features/ai/ai-suggest-replies-dialog/ai-suggest-replies-dialog.ts`
- Modify: `src/client/src/app/features/tickets/ticket-detail/ticket-detail.ts`
- Modify: `src/client/src/assets/i18n/en.json`
- Modify: `src/client/src/assets/i18n/ar.json`

**Interfaces:**
- Consumes: `ApiService` (core), `AiSuggestionDto`, `AiCategorizationResult`, `AiSummaryResult`, `AiSuggestedRepliesResult` (from backend Task 2), `MatDialog` (Angular Material)
- Produces:
  - `AiService` — HTTP client for AI endpoints
  - `AiSuggestionPanelComponent` — displays pending/historical AI suggestions with accept/reject
  - `AiSummaryComponent` — displays AI-generated ticket summary
  - `AiSuggestRepliesDialogComponent` — dialog for selecting AI-suggested replies
  - i18n keys for AI features in both EN and AR

- [ ] **Step 1: Create AiService**

```typescript
// src/client/src/app/features/ai/ai.service.ts
import { Injectable } from '@angular/core';
import { ApiService } from '../../core/services/api.service';
import { Observable } from 'rxjs';

export interface AiSuggestionDto {
  id: string;
  ticketId: string;
  type: string;
  output: string;
  confidence: number | null;
  status: string;
  appliedAt: string | null;
  model: string;
  tokensUsed: number;
  createdAt: string;
}

export interface AiCategorizationResult {
  suggestedCategoryId: string | null;
  suggestedCategoryName: string | null;
  suggestedPriorityId: string | null;
  suggestedPriorityName: string | null;
  confidence: number;
  autoApplied: boolean;
  suggestionId: string;
}

export interface AiSummaryResult {
  summary: string;
  suggestionId: string;
}

export interface AiSuggestedRepliesResult {
  suggestions: string[];
}

@Injectable({ providedIn: 'root' })
export class AiService extends ApiService {
  categorize(ticketId: string): Observable<AiCategorizationResult> {
    return this.post<AiCategorizationResult>(`/v1/Ai/tickets/${ticketId}/categorize`, {});
  }

  summarize(ticketId: string): Observable<AiSummaryResult> {
    return this.post<AiSummaryResult>(`/v1/Ai/tickets/${ticketId}/summarize`, {});
  }

  suggestReplies(ticketId: string): Observable<AiSuggestedRepliesResult> {
    return this.post<AiSuggestedRepliesResult>(`/v1/Ai/tickets/${ticketId}/suggest-replies`, {});
  }

  getSuggestions(ticketId: string): Observable<AiSuggestionDto[]> {
    return this.get<AiSuggestionDto[]>(`/v1/Ai/tickets/${ticketId}/suggestions`);
  }

  acceptSuggestion(suggestionId: string): Observable<any> {
    return this.put(`/v1/Ai/suggestions/${suggestionId}/accept`, {});
  }

  rejectSuggestion(suggestionId: string): Observable<any> {
    return this.put(`/v1/Ai/suggestions/${suggestionId}/reject`, {});
  }
}
```

- [ ] **Step 2: Create AiSuggestionPanelComponent**

```typescript
// src/client/src/app/features/ai/ai-suggestion-panel/ai-suggestion-panel.ts
import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { AiService, AiSuggestionDto } from '../ai.service';

@Component({
  selector: 'app-ai-suggestion-panel',
  imports: [DatePipe, TranslateModule, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule],
  template: `
    @if (suggestions.length > 0) {
      <mat-card>
        <mat-card-header>
          <mat-card-title>
            <mat-icon>psychology</mat-icon>
            {{ 'ai.suggestions' | translate }}
          </mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @for (suggestion of suggestions; track suggestion.id) {
            <div class="suggestion-item">
              <div class="suggestion-header">
                <mat-chip>{{ suggestion.type }}</mat-chip>
                @if (suggestion.confidence !== null) {
                  <mat-chip [color]="getConfidenceColor(suggestion.confidence)" selected>
                    {{ (suggestion.confidence * 100).toFixed(0) }}%
                  </mat-chip>
                }
                <span class="suggestion-status">{{ suggestion.status }}</span>
                <span class="suggestion-date">{{ suggestion.createdAt | date: 'short' }}</span>
              </div>
              <p class="suggestion-output">{{ suggestion.output }}</p>
              @if (suggestion.status === 'Pending') {
                <div class="suggestion-actions">
                  <button mat-raised-button color="primary" (click)="onAccept(suggestion.id)">
                    <mat-icon>check</mat-icon> {{ 'ai.accept' | translate }}
                  </button>
                  <button mat-button color="warn" (click)="onReject(suggestion.id)">
                    <mat-icon>close</mat-icon> {{ 'ai.reject' | translate }}
                  </button>
                </div>
              }
            </div>
          }
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: [`
    .suggestion-item { border-block-end: 1px solid rgba(0,0,0,0.12); padding-block: 12px; }
    .suggestion-item:last-child { border-block-end: none; }
    .suggestion-header { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .suggestion-status { font-size: 12px; font-weight: 500; }
    .suggestion-date { margin-inline-start: auto; font-size: 12px; color: rgba(0,0,0,0.6); }
    .suggestion-output { margin-block: 8px; font-size: 14px; white-space: pre-wrap; }
    .suggestion-actions { display: flex; gap: 8px; }
    mat-card-title { display: flex; align-items: center; gap: 8px; }
  `]
})
export class AiSuggestionPanelComponent {
  private aiService = inject(AiService);

  @Input() suggestions: AiSuggestionDto[] = [];
  @Output() updated = new EventEmitter<void>();

  getConfidenceColor(confidence: number): 'primary' | 'accent' | 'warn' {
    if (confidence >= 0.8) return 'primary';
    if (confidence >= 0.5) return 'accent';
    return 'warn';
  }

  onAccept(suggestionId: string): void {
    this.aiService.acceptSuggestion(suggestionId).subscribe(() => this.updated.emit());
  }

  onReject(suggestionId: string): void {
    this.aiService.rejectSuggestion(suggestionId).subscribe(() => this.updated.emit());
  }
}
```

- [ ] **Step 3: Create AiSummaryComponent**

```typescript
// src/client/src/app/features/ai/ai-summary/ai-summary.ts
import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AiService } from '../ai.service';

@Component({
  selector: 'app-ai-summary',
  imports: [TranslateModule, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>
          <mat-icon>summarize</mat-icon>
          {{ 'ai.summary' | translate }}
        </mat-card-title>
        <button mat-icon-button (click)="onGenerate()" [disabled]="loading">
          <mat-icon>refresh</mat-icon>
        </button>
      </mat-card-header>
      <mat-card-content>
        @if (loading) {
          <mat-spinner diameter="24"></mat-spinner>
        } @else if (summary) {
          <p class="summary-text">{{ summary }}</p>
        } @else {
          <p class="no-summary">{{ 'ai.noSummary' | translate }}</p>
          <button mat-raised-button color="primary" (click)="onGenerate()">
            <mat-icon>auto_awesome</mat-icon>
            {{ 'ai.generateSummary' | translate }}
          </button>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    mat-card-header { display: flex; align-items: center; }
    mat-card-header button { margin-inline-start: auto; }
    mat-card-title { display: flex; align-items: center; gap: 8px; }
    .summary-text { font-size: 14px; line-height: 1.6; white-space: pre-wrap; }
    .no-summary { color: rgba(0,0,0,0.6); font-size: 14px; }
  `]
})
export class AiSummaryComponent {
  private aiService = inject(AiService);

  @Input() ticketId = '';
  @Input() summary: string | null = null;
  @Output() summaryGenerated = new EventEmitter<string>();

  loading = false;

  onGenerate(): void {
    if (!this.ticketId) return;
    this.loading = true;
    this.aiService.summarize(this.ticketId).subscribe({
      next: result => {
        this.summary = result.summary;
        this.loading = false;
        this.summaryGenerated.emit(result.summary);
      },
      error: () => { this.loading = false; }
    });
  }
}
```

- [ ] **Step 4: Create AiSuggestRepliesDialogComponent**

```typescript
// src/client/src/app/features/ai/ai-suggest-replies-dialog/ai-suggest-replies-dialog.ts
import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AiService } from '../ai.service';

@Component({
  selector: 'app-ai-suggest-replies-dialog',
  imports: [TranslateModule, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>auto_awesome</mat-icon>
      {{ 'ai.suggestReplies' | translate }}
    </h2>
    <mat-dialog-content>
      @if (loading) {
        <div class="loading-container">
          <mat-spinner diameter="32"></mat-spinner>
          <p>{{ 'ai.generating' | translate }}</p>
        </div>
      } @else if (suggestions.length > 0) {
        @for (suggestion of suggestions; track $index) {
          <div class="reply-option">
            <p>{{ suggestion }}</p>
            <div class="reply-actions">
              <button mat-raised-button color="primary" (click)="onUse(suggestion)">
                <mat-icon>check</mat-icon> {{ 'ai.useReply' | translate }}
              </button>
            </div>
          </div>
        }
      } @else {
        <p>{{ 'ai.noSuggestions' | translate }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.close' | translate }}</button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: 8px; }
    .loading-container { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 24px; }
    .reply-option { border: 1px solid rgba(0,0,0,0.12); border-radius: 8px; padding: 12px; margin-block-end: 12px; }
    .reply-option p { margin: 0 0 8px; font-size: 14px; line-height: 1.5; white-space: pre-wrap; }
    .reply-actions { display: flex; gap: 8px; justify-content: flex-end; }
  `]
})
export class AiSuggestRepliesDialogComponent {
  private aiService = inject(AiService);
  private dialogRef = inject(MatDialogRef<AiSuggestRepliesDialogComponent>);
  private data: { ticketId: string } = inject(MAT_DIALOG_DATA);

  suggestions: string[] = [];
  loading = true;

  ngOnInit(): void {
    this.aiService.suggestReplies(this.data.ticketId).subscribe({
      next: result => {
        this.suggestions = result.suggestions;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  onUse(reply: string): void {
    this.dialogRef.close(reply);
  }
}
```

- [ ] **Step 5: Modify ticket-detail.ts to embed AI components**

Add imports to the top of `src/client/src/app/features/tickets/ticket-detail/ticket-detail.ts`:

```typescript
import { MatDialog } from '@angular/material/dialog';
import { AiService, AiSuggestionDto } from '../../ai/ai.service';
import { AiSuggestionPanelComponent } from '../../ai/ai-suggestion-panel/ai-suggestion-panel';
import { AiSummaryComponent } from '../../ai/ai-summary/ai-summary';
import { AiSuggestRepliesDialogComponent } from '../../ai/ai-suggest-replies-dialog/ai-suggest-replies-dialog';
```

Add to the `imports` array of `@Component`:

```typescript
AiSuggestionPanelComponent, AiSummaryComponent
```

Add to the `@Component` class body:

```typescript
private aiService = inject(AiService);
private dialog = inject(MatDialog);

aiSuggestions: AiSuggestionDto[] = [];
aiSummary: string | null = null;
categorizing = false;
```

Add a method to load AI data:

```typescript
loadAiData(ticketId: string): void {
  this.aiService.getSuggestions(ticketId).subscribe(suggestions => {
    this.aiSuggestions = suggestions;
    const latestSummary = suggestions.find(s => s.type === 'Summary');
    if (latestSummary) this.aiSummary = latestSummary.output;
  });
}
```

In the existing `loadTicket` method, after `this.ticket = ticket;` add:

```typescript
this.loadAiData(ticket.id);
```

Add AI action methods:

```typescript
onAiCategorize(): void {
  if (!this.ticket) return;
  this.categorizing = true;
  this.aiService.categorize(this.ticket.id).subscribe({
    next: () => {
      this.categorizing = false;
      this.loadTicket(this.ticket!.id);
    },
    error: () => { this.categorizing = false; }
  });
}

onAiSuggestReplies(): void {
  if (!this.ticket) return;
  const dialogRef = this.dialog.open(AiSuggestRepliesDialogComponent, {
    width: '600px',
    data: { ticketId: this.ticket.id }
  });
  dialogRef.afterClosed().subscribe(reply => {
    if (reply) {
      this.commentForm.patchValue({ content: reply });
    }
  });
}

onAiSuggestionsUpdated(): void {
  if (this.ticket) {
    this.loadTicket(this.ticket.id);
  }
}
```

In the template, add after the `action-card` (after the closing `</mat-card>` for the action bar) and before the `<mat-tab-group>`:

```html
<!-- AI Section -->
<div class="ai-actions-bar">
  <button mat-raised-button (click)="onAiCategorize()" [disabled]="categorizing">
    <mat-icon>category</mat-icon>
    {{ 'ai.categorize' | translate }}
  </button>
  <button mat-raised-button (click)="onAiSuggestReplies()">
    <mat-icon>auto_awesome</mat-icon>
    {{ 'ai.suggestReplies' | translate }}
  </button>
</div>

<app-ai-summary
  [ticketId]="ticket.id"
  [summary]="aiSummary"
  (summaryGenerated)="aiSummary = $event">
</app-ai-summary>

<app-ai-suggestion-panel
  [suggestions]="aiSuggestions"
  (updated)="onAiSuggestionsUpdated()">
</app-ai-suggestion-panel>
```

Add to the styles:

```css
.ai-actions-bar { display: flex; gap: 8px; margin-block-end: 16px; }
```

- [ ] **Step 6: Add i18n keys to en.json**

Add the `"ai"` section to `src/client/src/assets/i18n/en.json`:

```json
"ai": {
  "suggestions": "AI Suggestions",
  "summary": "AI Summary",
  "noSummary": "No summary generated yet",
  "generateSummary": "Generate Summary",
  "categorize": "AI Categorize",
  "suggestReplies": "Suggest Replies",
  "accept": "Accept",
  "reject": "Reject",
  "useReply": "Use Reply",
  "generating": "Generating suggestions...",
  "noSuggestions": "No suggestions could be generated"
}
```

- [ ] **Step 7: Add i18n keys to ar.json**

Add the `"ai"` section to `src/client/src/assets/i18n/ar.json`:

```json
"ai": {
  "suggestions": "اقتراحات الذكاء الاصطناعي",
  "summary": "ملخص الذكاء الاصطناعي",
  "noSummary": "لم يتم إنشاء ملخص بعد",
  "generateSummary": "إنشاء ملخص",
  "categorize": "تصنيف بالذكاء الاصطناعي",
  "suggestReplies": "اقتراح ردود",
  "accept": "قبول",
  "reject": "رفض",
  "useReply": "استخدام الرد",
  "generating": "جاري إنشاء الاقتراحات...",
  "noSuggestions": "لم يتم إنشاء اقتراحات"
}
```

- [ ] **Step 8: Verify frontend build**

Run:
```bash
cd src/client && npx ng build --configuration=development
```
Expected: 0 errors.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(ai-ui): add AI suggestion panel, summary, and suggest replies dialog to ticket detail page"
```

---

### Task 4: AI Chatbot

**Files:**
- Create: `src/CustomerSupport.Domain/Interfaces/IAiChatbotService.cs`
- Create: `src/CustomerSupport.Infrastructure/Services/Ai/AiChatbotService.cs`
- Modify: `src/CustomerSupport.API/Hubs/ChatHub.cs`
- Modify: `src/CustomerSupport.Infrastructure/Services/ChatSessionService.cs`
- Modify: `src/CustomerSupport.Infrastructure/DependencyInjection.cs`
- Modify: `src/client/src/assets/i18n/en.json`
- Modify: `src/client/src/assets/i18n/ar.json`

**Interfaces:**
- Consumes: `IAiClient`, `IPromptTemplateService`, `AiRateLimiter`, `AiSettings` (from Task 1); `IKnowledgeRepository`, `AppDbContext`, `Conversation`, `Message`, `KnowledgeArticle` entities (from Phase 2-3); `IDateTimeService`; `ConversationCreatedNotification`, `MessageReceivedNotification` (from Phase 3)
- Produces:
  - `IAiChatbotService` interface + `ChatbotResponse` record
  - `AiChatbotService : IAiChatbotService` implementation
  - Modified `ChatHub.SendMessage` with chatbot routing
  - Modified `ChatSessionService.StartSessionAsync` to skip auto-assign when chatbot enabled
  - Chatbot i18n keys for EN and AR

- [ ] **Step 1: Create IAiChatbotService interface**

```csharp
// src/CustomerSupport.Domain/Interfaces/IAiChatbotService.cs
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
```

- [ ] **Step 2: Create AiChatbotService implementation**

```csharp
// src/CustomerSupport.Infrastructure/Services/Ai/AiChatbotService.cs
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
            try
            {
                response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
            }
            finally
            {
                _rateLimiter.Release(0);
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
            try
            {
                checkResponse = await _aiClient.ChatAsync(new AiChatRequest(checkPrompt, [], MaxTokens: 100), ct);
            }
            finally
            {
                _rateLimiter.Release(0);
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
```

- [ ] **Step 3: Modify ChatHub to add chatbot routing**

Replace the `SendMessage` method in `src/CustomerSupport.API/Hubs/ChatHub.cs` with chatbot-aware routing. The hub needs `IAiChatbotService`, `IChatSessionService`, and `IOptions<AiSettings>` injected.

Update the constructor and fields:

```csharp
using CustomerSupport.Infrastructure.Services.Ai;
using Microsoft.Extensions.Options;
// ... existing usings

[Authorize(AuthenticationSchemes = "Bearer,Portal")]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPublisher _publisher;
    private readonly IAiChatbotService _chatbotService;
    private readonly AiSettings _aiSettings;

    public ChatHub(
        AppDbContext context,
        IDateTimeService dateTimeService,
        IPublisher publisher,
        IAiChatbotService chatbotService,
        IOptions<AiSettings> aiSettings)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _publisher = publisher;
        _chatbotService = chatbotService;
        _aiSettings = aiSettings.Value;
    }
```

Replace the `SendMessage` method body:

```csharp
    public async Task SendMessage(Guid conversationId, string content)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null) return;

        var userId = GetUserId();
        var isAgent = Context.User?.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role) ?? false;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversationId,
            Direction = isAgent ? MessageDirection.Outbound : MessageDirection.Inbound,
            SenderType = isAgent ? SenderType.Agent : SenderType.Customer,
            SenderId = userId != Guid.Empty ? userId : null,
            Content = content,
            ContentType = ContentType.Text,
            Channel = ChannelType.LiveChat,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        await Clients.Group($"chat-{conversationId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.ConversationId,
            message.Direction,
            message.SenderType,
            message.SenderId,
            message.Content,
            message.SentAt
        });

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversationId, conversation.TenantId, message.Direction));

        if (!isAgent && conversation.AssignedAgentId == null && _aiSettings.ChatbotEnabled)
        {
            try
            {
                var botResponse = await _chatbotService.GenerateResponseAsync(
                    conversationId, content, conversation.TenantId);

                var botMessage = new Message
                {
                    Id = Guid.NewGuid(),
                    TenantId = conversation.TenantId,
                    ConversationId = conversationId,
                    Direction = MessageDirection.Outbound,
                    SenderType = SenderType.System,
                    Content = botResponse.Content,
                    ContentType = ContentType.Text,
                    Channel = ChannelType.LiveChat,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new { aiBot = true }),
                    SentAt = _dateTimeService.UtcNow
                };

                await _context.Messages.AddAsync(botMessage);
                await _context.SaveChangesAsync();

                await Clients.Group($"chat-{conversationId}").SendAsync("ReceiveMessage", new
                {
                    botMessage.Id,
                    botMessage.ConversationId,
                    botMessage.Direction,
                    botMessage.SenderType,
                    botMessage.SenderId,
                    botMessage.Content,
                    botMessage.SentAt
                });

                if (botResponse.ShouldEscalate)
                {
                    var assignmentService = Context.GetHttpContext()!.RequestServices
                        .GetRequiredService<CustomerSupport.Infrastructure.Services.AssignmentService>();
                    await assignmentService.AutoAssignAsync(conversation, _context);
                }
            }
            catch (Exception ex)
            {
                var logger = Context.GetHttpContext()!.RequestServices
                    .GetRequiredService<ILogger<ChatHub>>();
                logger.LogError(ex, "Chatbot failed for conversation {ConversationId}", conversationId);
            }
        }
    }
```

Add required usings at the top of ChatHub.cs:

```csharp
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
```

- [ ] **Step 4: Modify ChatSessionService to skip auto-assign when chatbot enabled**

In `src/CustomerSupport.Infrastructure/Services/ChatSessionService.cs`, inject `IOptions<AiSettings>` and update `StartSessionAsync`.

Update the class:

```csharp
using CustomerSupport.Infrastructure.Services.Ai;
using Microsoft.Extensions.Options;
// ... existing usings

public class ChatSessionService : IChatSessionService
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly AiSettings _aiSettings;

    public ChatSessionService(
        AppDbContext context,
        IDateTimeService dateTimeService,
        IOptions<AiSettings> aiSettings)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _aiSettings = aiSettings.Value;
    }
```

The `StartSessionAsync` method already does NOT auto-assign agents — it creates the conversation with `AssignedAgentId = null` (no `AssignedAgentId` set in the constructor). So the chatbot flow works as-is: new portal conversations have no agent, and `ChatHub.SendMessage` routes to the chatbot when `AssignedAgentId == null && ChatbotEnabled`. No logic change needed in this method — only the constructor injection update.

- [ ] **Step 5: Register AiChatbotService in DependencyInjection.cs**

Add to `src/CustomerSupport.Infrastructure/DependencyInjection.cs` after the `IAiTicketService` registration (from Task 2):

```csharp
services.AddScoped<IAiChatbotService, AiChatbotService>();
```

- [ ] **Step 6: Add chatbot i18n keys to en.json**

Add the `"chat.bot"` keys to `src/client/src/assets/i18n/en.json` inside the existing `"chat"` section:

```json
"bot": {
  "name": "Support Bot",
  "connecting": "Connecting you to an agent...",
  "greeting": "Hello! I'm your support assistant. How can I help?"
}
```

- [ ] **Step 7: Add chatbot i18n keys to ar.json**

Add the `"chat.bot"` keys to `src/client/src/assets/i18n/ar.json` inside the existing `"chat"` section:

```json
"bot": {
  "name": "بوت الدعم",
  "connecting": "جاري توصيلك بأحد الموظفين...",
  "greeting": "مرحبا! أنا مساعد الدعم. كيف يمكنني مساعدتك؟"
}
```

- [ ] **Step 8: Verify full build**

Run:
```bash
dotnet build src/CustomerSupport.API/CustomerSupport.API.csproj
```
Expected: 0 errors.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(chatbot): add AI chatbot with RAG-based knowledge retrieval and human escalation"
```
