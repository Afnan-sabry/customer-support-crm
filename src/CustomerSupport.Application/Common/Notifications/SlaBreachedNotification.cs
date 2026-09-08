using MediatR;

namespace CustomerSupport.Application.Common.Notifications;

public record SlaBreachedNotification(
    Guid TicketId, Guid TenantId, string BreachType, Guid SlaPolicyId) : INotification;
