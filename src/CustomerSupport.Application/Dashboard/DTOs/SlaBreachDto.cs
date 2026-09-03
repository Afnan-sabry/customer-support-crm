namespace CustomerSupport.Application.Dashboard.DTOs;

public record SlaBreachDto(Guid TicketId, string TicketNumber, string BreachType, string PolicyName, DateTime DueAt, DateTime BreachedAt, double MinutesLate);
