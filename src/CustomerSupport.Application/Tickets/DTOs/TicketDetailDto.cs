namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketDetailDto(
    Guid Id, string TicketNumber, Guid CustomerId, string CustomerName,
    Guid CategoryId, string CategoryName, Guid PriorityId, string PriorityName,
    Guid StatusId, string StatusName, Guid? AssignedToId, string? AssignedToName,
    string Subject, string Description, DateTime CreatedAt, DateTime UpdatedAt,
    List<TicketCommentDto> Comments, List<TicketAttachmentDto> Attachments, List<TicketHistoryDto> History);
