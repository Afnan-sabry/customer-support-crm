using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketStatusChangedNotification(
    Guid TicketId, Guid TenantId, Guid OldStatusId, Guid NewStatusId) : INotification;
