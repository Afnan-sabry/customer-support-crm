using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketCommentAddedNotification(
    Guid TicketId, Guid CommentUserId,
    Guid TenantId) : INotification;
