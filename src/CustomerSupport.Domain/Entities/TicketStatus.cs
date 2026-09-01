namespace CustomerSupport.Domain.Entities;

public class TicketStatus : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsFinal { get; set; }
    public bool IsActive { get; set; } = true;
}
