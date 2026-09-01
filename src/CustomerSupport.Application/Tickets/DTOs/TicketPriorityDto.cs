namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketPriorityDto(Guid Id, string Name, string NameAr, int Level);
