namespace CustomerSupport.Application.Integrations.DTOs;

public record WebhookSubscriptionDto(
    Guid Id, string Name, string Url, string[] Events,
    bool IsActive, DateTime CreatedAt);

public record WebhookDeliveryLogDto(
    Guid Id, string Event, int? StatusCode, bool Success,
    int Attempt, string? ErrorMessage, DateTime CreatedAt);
