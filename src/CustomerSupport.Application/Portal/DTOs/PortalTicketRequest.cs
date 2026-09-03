namespace CustomerSupport.Application.Portal.DTOs;

public record PortalTicketRequest(Guid CategoryId, Guid PriorityId, string Subject, string Description);
