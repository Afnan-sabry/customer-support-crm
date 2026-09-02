using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketCreatedNotification(
    Guid TicketId, Guid TenantId,
    Guid PriorityId, Guid CategoryId) : INotification;
