namespace CustomerSupport.Domain.Entities;

public class WebhookDeliveryLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int Attempt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public WebhookSubscription Subscription { get; set; } = null!;
}
