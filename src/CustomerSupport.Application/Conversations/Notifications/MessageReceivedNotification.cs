using CustomerSupport.Domain.Enums;
using MediatR;

namespace CustomerSupport.Application.Conversations.Notifications;

public record MessageReceivedNotification(
    Guid MessageId, Guid ConversationId, Guid TenantId,
    MessageDirection Direction) : INotification;
