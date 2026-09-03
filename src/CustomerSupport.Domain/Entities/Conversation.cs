using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Conversation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? TicketId { get; set; }
    public ChannelType Channel { get; set; }
    public ConversationStatus Status { get; set; }
    public string? Subject { get; set; }
    public string? ExternalReference { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Customer? Customer { get; set; }
    public Ticket? Ticket { get; set; }
    public ApplicationUser? AssignedAgent { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
