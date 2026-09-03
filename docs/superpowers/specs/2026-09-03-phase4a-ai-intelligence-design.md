# Phase 4A — AI Intelligence: Design Specification

**Date:** 2026-09-03
**Status:** Approved
**Author:** Afnan Sabry / Claude
**Parent Spec:** `docs/superpowers/specs/2026-08-31-customer-support-crm-design.md`

---

## 1. Overview

Phase 4A adds AI-powered intelligence to the Customer Support CRM: a foundational AI service layer, AI-driven ticket features (auto-categorization, summarization, suggested replies), and a customer-facing AI chatbot integrated into the portal live chat. It builds on the ticket domain (Phase 1), knowledge base (Phase 2), and conversation/channel infrastructure (Phase 3).

**Key decisions:**
- Thin abstraction over Azure OpenAI with swappable mock provider (same pattern as `IEmailSender`/`MockEmailSender`)
- RAG-based chatbot using existing knowledge base articles — no vector DB for MVP
- AI suggestions stored in an `AiSuggestion` audit entity with confidence scores and accept/reject tracking
- Chatbot reuses existing conversation/message domain — no separate memory store
- In-process rate limiting via `SemaphoreSlim` — no Redis dependency

## 2. Scope

| Task | Description |
|------|-------------|
| P4.1 | AI Service Foundation — Azure OpenAI client wrapper, prompt templates, rate limiting |
| P4.2 | AI Ticket Features — Auto-categorization, summarization, suggested replies |
| P4.3 | AI Chatbot — Customer-facing chatbot, knowledge base-aware, escalation to human |

**Dependencies:**
- P4.1 first (foundation for P4.2 and P4.3)
- P4.2 and P4.3 independent after P4.1

**External dependency:** Azure OpenAI SDK (`Azure.AI.OpenAI` NuGet package)

## 3. AI Service Foundation (P4.1)

### 3.1 IAiClient Interface

The core abstraction for all AI interactions. Defined in Domain/Interfaces:

```csharp
public interface IAiClient
{
    Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default);
    Task<AiEmbeddingResponse> GetEmbeddingAsync(string text, CancellationToken ct = default);
}

public record AiChatRequest(
    string SystemPrompt,
    List<AiChatMessage> Messages,
    float Temperature = 0.3f,
    int MaxTokens = 1024);

public record AiChatMessage(string Role, string Content);

public record AiChatResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    string Model);

public record AiEmbeddingResponse(
    float[] Embedding,
    int TokensUsed);
```

**Design rationale:** The interface is provider-agnostic — `AiChatRequest`/`AiChatResponse` use simple records, not Azure SDK types. This allows swapping Azure OpenAI for another provider by changing only the Infrastructure implementation.

### 3.2 AzureOpenAiClient Implementation

Located in `Infrastructure/Services/Ai/AzureOpenAiClient.cs`:

- Wraps `Azure.AI.OpenAI.AzureOpenAIClient` SDK
- Reads configuration from `IOptions<AiSettings>`
- Maps `AiChatRequest` → SDK `ChatCompletionOptions`, SDK response → `AiChatResponse`
- Handles transient errors with Polly retry policy (3 retries, exponential backoff)
- Logs all requests/responses at Debug level for troubleshooting

### 3.3 MockAiClient

Located in `Infrastructure/Services/Ai/MockAiClient.cs`:

- Returns canned responses based on system prompt keywords
- Logs the full prompt to `ILogger` for development visibility
- Simulates latency with configurable delay (default 500ms)
- Returns realistic token counts for cost estimation testing

### 3.4 AiSettings Configuration

```json
{
  "AiSettings": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://<resource>.openai.azure.com/",
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
}
```

Development override in `appsettings.Development.json` sets `Provider: "Mock"` to use `MockAiClient`.

Bound via `IOptions<AiSettings>` pattern — registered in `DependencyInjection.cs`. API keys use user-secrets in development, environment variables in production.

### 3.5 Prompt Template Service

Located in `Infrastructure/Services/Ai/PromptTemplateService.cs`:

```csharp
public interface IPromptTemplateService
{
    string GetTemplate(string key);
    string Render(string key, Dictionary<string, string> placeholders);
}
```

Templates stored as a `Dictionary<string, string>` in code — no DB storage. Keys:

| Key | Purpose | Placeholders |
|-----|---------|--------------|
| `ticket.categorize` | Classify ticket into category + priority | `{subject}`, `{description}`, `{categories}`, `{priorities}` |
| `ticket.summarize` | Summarize ticket conversation | `{subject}`, `{description}`, `{comments}` |
| `ticket.suggest-reply` | Generate reply suggestions | `{subject}`, `{description}`, `{recentMessages}`, `{knowledgeContext}` |
| `chatbot.answer` | Answer customer question from KB | `{question}`, `{conversationHistory}`, `{knowledgeArticles}` |
| `chatbot.escalation-check` | Determine if escalation needed | `{question}`, `{botResponse}` |

Templates are designed to produce structured JSON output where parsing is needed (categorization returns `{"categoryId": "...", "priorityId": "...", "confidence": 0.9}`).

### 3.6 Rate Limiting

Located in `Infrastructure/Services/Ai/AiRateLimiter.cs`:

- `SemaphoreSlim` for max concurrent requests (from `AiSettings.MaxConcurrentRequests`)
- Sliding window token counter for per-minute token budget (from `AiSettings.MaxTokensPerMinute`)
- `AcquireAsync(estimatedTokens, ct)` — blocks until capacity available or timeout
- `Report(actualTokens)` — adjusts the sliding window after request completes
- Injected as Singleton, wraps `IAiClient` calls in the service layer

### 3.7 Permissions

| Permission | Group | Description |
|------------|-------|-------------|
| `ai.use` | AI | Trigger AI features (categorize, summarize, suggest) |
| `ai.manage` | AI | Configure AI settings (future admin UI) |

Added to `PermissionSeeder.cs`. `ai.use` added to Agent role in `RoleAndUserSeeder.cs`.

## 4. AI Ticket Features (P4.2)

### 4.1 AiSuggestion Entity

New domain entity for audit trail of all AI interactions with tickets:

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | Guid | PK | |
| TenantId | Guid | FK, indexed | Global query filter |
| TicketId | Guid | FK→Ticket | |
| Type | string(50) | Required | "Categorization", "PrioritySuggestion", "Summary" |
| Input | string | Required | Truncated prompt context (max 2000 chars) |
| Output | string | Required | AI response text |
| Confidence | decimal(5,4)? | Nullable | 0-1 scale, null for summaries |
| Status | string(20) | Required | "Pending", "Accepted", "Rejected", "AutoApplied" |
| AppliedAt | DateTime? | Nullable | When accepted or auto-applied |
| Model | string(100) | Required | Model deployment name used |
| TokensUsed | int | Required | Total tokens for cost tracking |

**Indexes:**
- `(TenantId, TicketId)` — lookup suggestions for a ticket
- `(TenantId, Type, Status)` — analytics on AI usage

**EF Configuration:** `IEntityTypeConfiguration<AiSuggestion>` in Infrastructure.

### 4.2 IAiTicketService

Facade interface in Domain/Interfaces:

```csharp
public interface IAiTicketService
{
    Task<AiCategorizationResult> CategorizeAsync(Guid ticketId, CancellationToken ct = default);
    Task<AiSummaryResult> SummarizeAsync(Guid ticketId, CancellationToken ct = default);
    Task<AiSuggestedRepliesResult> SuggestRepliesAsync(Guid ticketId, CancellationToken ct = default);
}

public record AiCategorizationResult(
    Guid? SuggestedCategoryId, string? SuggestedCategoryName,
    Guid? SuggestedPriorityId, string? SuggestedPriorityName,
    decimal Confidence, bool AutoApplied, Guid SuggestionId);

public record AiSummaryResult(string Summary, Guid SuggestionId);

public record AiSuggestedRepliesResult(List<string> Suggestions);
```

### 4.3 Auto-Categorization

**Automatic trigger:** `AiCategorizationHandler : INotificationHandler<TicketCreatedNotification>`

Flow:
1. Ticket created → handler fires
2. Load ticket `Subject` + `Description`
3. Load tenant's active categories and priorities (names + IDs)
4. Render `ticket.categorize` prompt template with context
5. Send to `IAiClient.ChatAsync` — AI returns JSON with `categoryId`, `priorityId`, `confidence`
6. Create `AiSuggestion` entity with result
7. If confidence ≥ `AiSettings.CategorizationAutoApplyThreshold` (default 0.8):
   - Update ticket's `CategoryId` and `PriorityId`
   - Set suggestion status = "AutoApplied"
   - Record change in `TicketHistory`
8. If confidence < threshold:
   - Set suggestion status = "Pending"
   - Agent reviews in UI

**Manual trigger:** `CategorizeTicketCommand` — same logic but always stores as "Pending" for agent review (does not auto-apply).

**Error handling:** If AI call fails, log the error and skip — ticket creation succeeds without AI categorization. Non-blocking.

### 4.4 Summarization

**On-demand only.** `SummarizeTicketCommand`:

1. Load ticket with all comments (ordered by date)
2. Truncate to fit context window (keep subject, description, last N comments)
3. Render `ticket.summarize` prompt template
4. Send to AI, receive summary text
5. Store as `AiSuggestion` (type="Summary", status="AutoApplied", confidence=null)
6. Return summary text

If a summary already exists for this ticket and no new comments since, return the cached summary.

### 4.5 Suggested Replies

**On-demand.** `SuggestReplyCommand`:

1. Load ticket context (subject, description, recent messages/comments)
2. Search knowledge base articles by ticket subject + category name (reuse existing search query pattern, top 5 results)
3. Render `ticket.suggest-reply` prompt template with ticket context + KB articles
4. Send to AI — prompt instructs to return 1-3 distinct reply options as JSON array
5. Return suggestions directly — **not stored** (ephemeral, agent picks or discards)

### 4.6 Accept/Reject Suggestions

`AcceptAiSuggestionCommand(Guid SuggestionId)`:
- Updates suggestion status to "Accepted"
- Applies the suggested change (e.g., updates ticket CategoryId/PriorityId)
- Records change in `TicketHistory`

`RejectAiSuggestionCommand(Guid SuggestionId)`:
- Updates suggestion status to "Rejected"
- No ticket changes

### 4.7 API Endpoints

New `AiController` at `api/v1/ai`:

| Method | Route | Permission | Description |
|--------|-------|------------|-------------|
| POST | `/ai/tickets/{id}/categorize` | ai.use | Trigger manual categorization |
| POST | `/ai/tickets/{id}/summarize` | ai.use | Generate ticket summary |
| POST | `/ai/tickets/{id}/suggest-replies` | ai.use | Get reply suggestions |
| GET | `/ai/tickets/{id}/suggestions` | ai.use | List AI suggestions for ticket |
| PUT | `/ai/suggestions/{id}/accept` | ai.use | Accept a pending suggestion |
| PUT | `/ai/suggestions/{id}/reject` | ai.use | Reject a pending suggestion |

### 4.8 Angular UI Additions

**Ticket detail page** — add an "AI" section:

- **AI Actions toolbar:** Three buttons — "Categorize", "Summarize", "Suggest Reply". Each triggers the corresponding API call with a loading spinner.
- **Suggestions panel:** Shows pending `AiSuggestion` records for the ticket. Each displays the suggestion type, output, confidence badge (high/medium/low), and Accept/Reject buttons. Accepted/Rejected suggestions shown in a collapsed history section.
- **Summary section:** Displayed below ticket description when a summary exists. "Regenerate" button to create fresh summary.
- **Suggested replies dialog:** Modal/overlay showing 1-3 reply options. Each has a "Use" button that copies the text into the reply editor, and an "Edit & Use" button for modification.

**New Angular files:**
- `features/ai/ai.service.ts` — extends `ApiService`
- `features/ai/ai-suggestion-panel/ai-suggestion-panel.ts` — standalone component
- `features/ai/ai-summary/ai-summary.ts` — standalone component
- `features/ai/ai-suggest-replies-dialog/ai-suggest-replies-dialog.ts` — dialog component

These are **embedded components** used on the ticket detail page, not a separate routed feature. No new route needed.

## 5. AI Chatbot (P4.3)

### 5.1 IAiChatbotService

Interface in Domain/Interfaces:

```csharp
public interface IAiChatbotService
{
    Task<ChatbotResponse> GenerateResponseAsync(
        Guid conversationId, string userMessage, Guid tenantId,
        CancellationToken ct = default);
}

public record ChatbotResponse(
    string Content,
    bool ShouldEscalate,
    string? EscalationReason);
```

### 5.2 RAG Architecture

On each user message:

1. **Retrieve:** Search knowledge base articles using keyword matching on the user's message (reuse existing `SearchKnowledgeArticlesQuery` pattern — `Contains()` search across Title, Content, Tags). Take top N articles (configurable via `AiSettings.ChatbotKnowledgeArticleCount`, default 5).
2. **Augment:** Build prompt with:
   - System prompt from `chatbot.answer` template
   - Knowledge articles as context (title + content, truncated to fit)
   - Last N conversation messages for continuity (from existing `Message` entity, default 10)
   - Current user message
3. **Generate:** Send to `IAiClient.ChatAsync`, receive response
4. **Confidence check:** Use `chatbot.escalation-check` template to assess if the bot's answer is grounded in the KB context. If confidence is below `AiSettings.ChatbotConfidenceThreshold`, set `ShouldEscalate = true`.

### 5.3 Conversation Flow

```
Portal user sends message via ChatHub
        │
        ▼
  Has human agent assigned?
        │
   yes ──┼── no
   │     │
   │     ▼
   │  Is chatbot enabled? (AiSettings.ChatbotEnabled)
   │     │
   │  yes ──┼── no
   │  │     │
   │  │     ▼
   │  │  Auto-assign to agent (existing flow)
   │  │
   │  ▼
   │  Check escalation triggers:
   │  - Keyword match ("agent", "human", "speak to someone")
   │  - Max bot turns reached (AiSettings.ChatbotMaxTurns)
   │     │
   │  trigger ──┼── no trigger
   │  │         │
   │  │         ▼
   │  │      IAiChatbotService.GenerateResponseAsync
   │  │         │
   │  │      ShouldEscalate?
   │  │         │
   │  │      yes ──┼── no
   │  │      │     │
   │  ◄──────┘     ▼
   │              Save bot message (SenderType.System, metadata: {"aiBot": true})
   │              Send to customer via SignalR
   │
   ▼
  Escalate:
  1. Trigger auto-assignment (existing AssignmentService)
  2. Send system message: "Connecting you to an agent..."
  3. All future messages route to human agent
```

### 5.4 ChatHub Modifications

Modify `ChatHub.SendMessage` method:

- Before forwarding to agent group, check if conversation has `AssignedAgentId == null`
- If null and chatbot enabled, invoke `IAiChatbotService` instead of forwarding
- Save bot response as `Message` entity with `SenderType.System`
- Track bot turn count via message count where `SenderType == System && metadata contains aiBot`
- On escalation, call `AssignConversationCommand` or `IChatSessionService` auto-assignment

### 5.5 ChatSessionService Modification

`StartSessionAsync` — when `AiSettings.ChatbotEnabled` is true for portal-initiated chats:
- Do NOT auto-assign an agent
- Leave `AssignedAgentId = null` so the chatbot handles the conversation
- Agent assignment happens only on escalation

### 5.6 Bot Message Rendering

No new Angular components needed. The portal chat widget already renders messages from any sender. Bot messages are distinguished by:
- `SenderType.System` with `metadata.aiBot = true`
- Display name: "Support Bot" (from i18n: `chat.bot.name` / `chat.bot.nameAr`)
- Bot avatar: a distinct icon (robot/AI icon) vs agent avatar

Add i18n keys for bot messages:
- `chat.bot.name` / `chat.bot.nameAr` — "Support Bot" / "بوت الدعم"
- `chat.bot.connecting` / `chat.bot.connectingAr` — "Connecting you to an agent..." / "جاري توصيلك بأحد الموظفين..."
- `chat.bot.greeting` / `chat.bot.greetingAr` — "Hello! I'm your support assistant. How can I help?" / "مرحبا! أنا مساعد الدعم. كيف يمكنني مساعدتك؟"

## 6. File Map

### Backend — New Files

**P4.1 — AI Service Foundation:**
```
src/CustomerSupport.Domain/
  Interfaces/IAiClient.cs              — includes AiChatRequest, AiChatMessage, AiChatResponse, AiEmbeddingResponse records
  Interfaces/IPromptTemplateService.cs
src/CustomerSupport.Infrastructure/
  Services/Ai/AzureOpenAiClient.cs
  Services/Ai/MockAiClient.cs
  Services/Ai/AiRateLimiter.cs
  Services/Ai/PromptTemplateService.cs
  Services/Ai/PromptTemplates.cs
```

**P4.2 — AI Ticket Features:**
```
src/CustomerSupport.Domain/
  Entities/AiSuggestion.cs
  Interfaces/IAiTicketService.cs       — includes AiCategorizationResult, AiSummaryResult, AiSuggestedRepliesResult records
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

**P4.3 — AI Chatbot:**
```
src/CustomerSupport.Domain/
  Interfaces/IAiChatbotService.cs      — includes ChatbotResponse record
src/CustomerSupport.Infrastructure/
  Services/Ai/AiChatbotService.cs
```

### Backend — Modified Files

```
src/CustomerSupport.Infrastructure/Persistence/AppDbContext.cs — add AiSuggestions DbSet
src/CustomerSupport.Infrastructure/DependencyInjection.cs — register AI services
src/CustomerSupport.Infrastructure/Persistence/Seeders/PermissionSeeder.cs — add ai.use, ai.manage
src/CustomerSupport.Infrastructure/Persistence/Seeders/RoleAndUserSeeder.cs — add ai.use to Agent role
src/CustomerSupport.API/Program.cs — add AiSettings configuration binding
src/CustomerSupport.API/Hubs/ChatHub.cs — add chatbot routing logic
src/CustomerSupport.Infrastructure/Services/ChatSessionService.cs — skip auto-assign when chatbot enabled
src/CustomerSupport.API/appsettings.json — add AiSettings section
src/CustomerSupport.API/appsettings.Development.json — add AiSettings with Provider=Mock
```

### Frontend — New Files

```
src/client/src/app/
  features/ai/ai.service.ts
  features/ai/ai-suggestion-panel/ai-suggestion-panel.ts
  features/ai/ai-summary/ai-summary.ts
  features/ai/ai-suggest-replies-dialog/ai-suggest-replies-dialog.ts
```

### Frontend — Modified Files

```
src/client/src/app/features/tickets/ticket-detail/ticket-detail.ts — embed AI components
src/client/src/assets/i18n/en.json — add AI and chatbot keys
src/client/src/assets/i18n/ar.json — add Arabic translations
```

## 7. Configuration Summary

| Setting | Default | Dev Override | Notes |
|---------|---------|-------------|-------|
| `AiSettings:Provider` | `"AzureOpenAI"` | `"Mock"` | Switches implementation |
| `AiSettings:Endpoint` | `""` | — | Azure OpenAI endpoint |
| `AiSettings:ApiKey` | `""` | user-secrets | Never in appsettings |
| `AiSettings:ChatDeployment` | `"gpt-4"` | — | Chat model deployment |
| `AiSettings:EmbeddingDeployment` | `"text-embedding-ada-002"` | — | Embedding model |
| `AiSettings:MaxConcurrentRequests` | `10` | — | Rate limiter |
| `AiSettings:MaxTokensPerMinute` | `60000` | — | Rate limiter |
| `AiSettings:DefaultTemperature` | `0.3` | — | Conservative for CRM |
| `AiSettings:DefaultMaxTokens` | `1024` | — | Response limit |
| `AiSettings:ChatbotEnabled` | `true` | — | Toggle chatbot |
| `AiSettings:ChatbotMaxTurns` | `5` | — | Before auto-escalation |
| `AiSettings:ChatbotConfidenceThreshold` | `0.6` | — | Below = escalate |
| `AiSettings:ChatbotKnowledgeArticleCount` | `5` | — | RAG retrieval count |
| `AiSettings:CategorizationAutoApplyThreshold` | `0.8` | — | Below = pending review |

## 8. Error Handling

- All AI calls are wrapped in try/catch — AI failures never block core CRM operations
- `AiTicketService` catches AI errors and logs them; categorization handler silently skips on failure
- `AiChatbotService` returns a fallback message on AI error: "I'm having trouble understanding. Let me connect you to an agent." → triggers escalation
- Rate limiter timeout (30s default) throws `AiRateLimitException` — caught by service layer, returns a "service busy" response
- Invalid AI JSON responses (failed parsing) are logged and treated as low-confidence results

## 9. Multi-Tenancy

- `AiSuggestion` entity has `TenantId` with global query filter (standard pattern)
- All AI service methods receive `TenantId` via `ICurrentUserService` (standard pattern)
- AI settings are global (not per-tenant for MVP) — tenant-specific AI configuration is a future enhancement
- Knowledge base search for chatbot RAG respects tenant filter automatically via EF global query filters

## 10. Testing Strategy

- Unit tests for `PromptTemplateService` — template rendering with placeholders
- Unit tests for `AiRateLimiter` — concurrent request limiting, token budget
- Integration tests for `AiTicketService` using `MockAiClient` — verify categorization, summarization, suggestion flows
- Integration tests for `AiChatbotService` using `MockAiClient` — verify RAG flow, escalation triggers
- No tests against real Azure OpenAI — all tests use mock client
