namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketStatusDto(Guid Id, string Name, string NameAr, int Order, bool IsFinal);
