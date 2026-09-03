namespace CustomerSupport.Domain.Entities;

public class AiSuggestion : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TicketId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public decimal? Confidence { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? AppliedAt { get; set; }
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }

    public Ticket? Ticket { get; set; }
}
