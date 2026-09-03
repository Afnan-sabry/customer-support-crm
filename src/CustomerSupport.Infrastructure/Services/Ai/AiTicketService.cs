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
        var actualTokens = 0;
        try
        {
            response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
            actualTokens = response.PromptTokens + response.CompletionTokens;
        }
        finally
        {
            _rateLimiter.Release(500, actualTokens);
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
        var actualTokens = 0;
        try
        {
            response = await _aiClient.ChatAsync(new AiChatRequest(prompt, []), ct);
            actualTokens = response.PromptTokens + response.CompletionTokens;
        }
        finally
        {
            _rateLimiter.Release(1500, actualTokens);
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
