namespace CustomerSupport.Domain.Entities;

public class TicketFeedback : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid CustomerId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
