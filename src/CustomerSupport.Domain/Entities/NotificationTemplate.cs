namespace CustomerSupport.Domain.Entities;

public class NotificationTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string SubjectAr { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string BodyTemplateAr { get; set; } = string.Empty;
    public string Channels { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}
