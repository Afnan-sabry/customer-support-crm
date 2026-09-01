namespace CustomerSupport.Domain.Entities;

public class Ticket : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid PriorityId { get; set; }
    public Guid StatusId { get; set; }
    public Guid? AssignedToId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Customer? Customer { get; set; }
    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketStatus? Status { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}
