using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookTicketStatusChangedHandler : INotificationHandler<TicketStatusChangedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookTicketStatusChangedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(TicketStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);
        if (ticket is null) return;

        var oldStatus = await _context.TicketStatuses.FindAsync([notification.OldStatusId], cancellationToken);
        var newStatus = await _context.TicketStatuses.FindAsync([notification.NewStatusId], cancellationToken);

        await _dispatcher.DispatchAsync(notification.TenantId, "ticket.status_changed", new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            oldStatus = oldStatus?.Name,
            newStatus = newStatus?.Name
        });

        if (newStatus?.IsFinal == true)
        {
            await _dispatcher.DispatchAsync(notification.TenantId, "ticket.resolved", new
            {
                ticketId = ticket.Id,
                ticketNumber = ticket.TicketNumber,
                resolvedAt = DateTime.UtcNow,
                resolutionMinutes = (DateTime.UtcNow - ticket.CreatedAt).TotalMinutes
            });
        }
    }
}
