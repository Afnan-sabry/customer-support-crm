namespace CustomerSupport.Application.Dashboard.DTOs;

public record CategoryDistributionDto(Guid CategoryId, string CategoryName, string CategoryNameAr, int TicketCount);
