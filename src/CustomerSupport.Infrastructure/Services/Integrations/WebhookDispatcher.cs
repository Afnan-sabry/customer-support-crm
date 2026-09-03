using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services.Integrations;

public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid tenantId, string eventName, object payload)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var subscriptions = await context.WebhookSubscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync();

        var matching = subscriptions
            .Where(s => JsonSerializer.Deserialize<string[]>(s.Events)?.Contains(eventName) == true)
            .ToList();

        var jsonPayload = JsonSerializer.Serialize(payload);

        foreach (var sub in matching)
        {
            _ = Task.Run(() => DeliverWithRetry(sub, tenantId, eventName, jsonPayload));
        }
    }

    private async Task DeliverWithRetry(WebhookSubscription subscription, Guid tenantId, string eventName, string jsonPayload)
    {
        var delays = new[] { 0, 2000, 8000 };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            if (attempt > 1)
                await Task.Delay(delays[attempt - 1]);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var signature = ComputeHmac(subscription.Secret, jsonPayload);

                var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                request.Headers.Add("X-Webhook-Event", eventName);

                if (!string.IsNullOrEmpty(subscription.Headers))
                {
                    var extraHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(subscription.Headers);
                    if (extraHeaders is not null)
                    {
                        foreach (var (key, value) in extraHeaders)
                            request.Headers.TryAddWithoutValidation(key, value);
                    }
                }

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                var log = new WebhookDeliveryLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = subscription.Id,
                    Event = eventName,
                    Payload = jsonPayload.Length > 65536 ? jsonPayload[..65536] : jsonPayload,
                    StatusCode = (int)response.StatusCode,
                    ResponseBody = responseBody.Length > 4000 ? responseBody[..4000] : responseBody,
                    Attempt = attempt,
                    Success = response.IsSuccessStatusCode
                };

                context.WebhookDeliveryLogs.Add(log);
                await context.SaveChangesAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook delivered: {Event} to {Url} (attempt {Attempt})", eventName, subscription.Url, attempt);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery failed: {Event} to {Url} (attempt {Attempt})", eventName, subscription.Url, attempt);

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = subscription.Id,
                    Event = eventName,
                    Payload = jsonPayload.Length > 65536 ? jsonPayload[..65536] : jsonPayload,
                    Attempt = attempt,
                    Success = false,
                    ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message
                });
                await context.SaveChangesAsync();
            }
        }
    }

    private static string ComputeHmac(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}
