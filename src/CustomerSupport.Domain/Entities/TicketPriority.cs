namespace CustomerSupport.Domain.Entities;

public class TicketPriority : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; } = true;
}
