using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookSlaBreachedHandler : INotificationHandler<SlaBreachedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookSlaBreachedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(SlaBreachedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);
        var policy = await _context.SlaPolicies.FindAsync([notification.SlaPolicyId], cancellationToken);
        if (ticket is null) return;

        await _dispatcher.DispatchAsync(notification.TenantId, "sla.breached", new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            breachType = notification.BreachType,
            policyName = policy?.Name,
            dueAt = DateTime.UtcNow
        });
    }
}
