namespace CustomerSupport.Domain.Entities;

public class EscalationRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public int TriggerAfterMinutes { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? ActionTarget { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}
