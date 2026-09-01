namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketDto(
    Guid Id, string TicketNumber, string Subject, string CustomerName,
    string CategoryName, string PriorityName, string StatusName,
    string? AssignedToName, DateTime CreatedAt);
