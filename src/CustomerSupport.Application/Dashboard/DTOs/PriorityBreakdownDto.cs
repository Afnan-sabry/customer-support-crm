namespace CustomerSupport.Application.Dashboard.DTOs;

public record PriorityBreakdownDto(Guid PriorityId, string PriorityName, string PriorityNameAr, int Level, int TicketCount);
