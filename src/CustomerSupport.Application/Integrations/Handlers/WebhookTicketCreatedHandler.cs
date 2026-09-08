using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookTicketCreatedHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookTicketCreatedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer).Include(t => t.Category).Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);
        if (ticket is null) return;

        await _dispatcher.DispatchAsync(notification.TenantId, "ticket.created", new
        {
            ticketId = ticket.Id,
            ticketNumber = ticket.TicketNumber,
            subject = ticket.Subject,
            categoryName = ticket.Category?.Name,
            priorityName = ticket.Priority?.Name,
            customerName = ticket.Customer?.Name
        });
    }
}
