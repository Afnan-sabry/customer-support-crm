namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketAttachmentDto(Guid Id, string FileName, string ContentType, long FileSize, DateTime CreatedAt);
