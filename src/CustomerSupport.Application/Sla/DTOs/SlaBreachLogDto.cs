namespace CustomerSupport.Application.Sla.DTOs;

public record SlaBreachLogDto(
    Guid Id, Guid TicketId, string TicketNumber,
    string BreachType, DateTime DueAt, DateTime BreachedAt);
