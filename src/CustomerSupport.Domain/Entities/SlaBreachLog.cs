namespace CustomerSupport.Domain.Entities;

public class SlaBreachLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid SlaPolicyId { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public DateTime DueAt { get; set; }
    public DateTime BreachedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public SlaPolicy SlaPolicy { get; set; } = null!;
}
