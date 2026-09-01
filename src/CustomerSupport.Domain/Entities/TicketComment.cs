namespace CustomerSupport.Domain.Entities;

public class TicketComment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public Ticket? Ticket { get; set; }
    public ApplicationUser? User { get; set; }
}
