namespace CustomerSupport.Domain.Entities;

public class SlaPolicy : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}
