using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record TicketEscalatedNotification(
    Guid TicketId, Guid TenantId, string Reason) : INotification;
