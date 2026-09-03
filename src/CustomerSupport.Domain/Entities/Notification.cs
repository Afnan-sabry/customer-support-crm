using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Notification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public RecipientType RecipientType { get; set; }
    public Guid RecipientId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
