namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketCommentDto(Guid Id, Guid UserId, string UserName, string Content, bool IsInternal, DateTime CreatedAt);
