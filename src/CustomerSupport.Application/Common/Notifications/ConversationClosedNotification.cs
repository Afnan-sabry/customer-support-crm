using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record ConversationClosedNotification(
    Guid ConversationId, Guid TenantId) : INotification;
