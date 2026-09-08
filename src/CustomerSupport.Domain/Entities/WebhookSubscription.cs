namespace CustomerSupport.Domain.Entities;

public class WebhookSubscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Events { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public string? Headers { get; set; }

    public ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}
