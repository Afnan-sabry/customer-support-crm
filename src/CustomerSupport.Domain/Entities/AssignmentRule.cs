namespace CustomerSupport.Domain.Entities;

public class AssignmentRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? PriorityId { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public string? AgentPool { get; set; }
    public int LastAssignedIndex { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
}
