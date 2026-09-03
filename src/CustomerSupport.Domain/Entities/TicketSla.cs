namespace CustomerSupport.Domain.Entities;

public class TicketSla : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public Guid SlaPolicyId { get; set; }
    public DateTime FirstResponseDue { get; set; }
    public DateTime ResolutionDue { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool FirstResponseBreached { get; set; }
    public bool ResolutionBreached { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public SlaPolicy SlaPolicy { get; set; } = null!;
}
