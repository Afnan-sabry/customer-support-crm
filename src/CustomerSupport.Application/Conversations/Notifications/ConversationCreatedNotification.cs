using CustomerSupport.Domain.Enums;
using MediatR;

namespace CustomerSupport.Application.Conversations.Notifications;

public record ConversationCreatedNotification(
    Guid ConversationId, Guid TenantId, ChannelType Channel) : INotification;
