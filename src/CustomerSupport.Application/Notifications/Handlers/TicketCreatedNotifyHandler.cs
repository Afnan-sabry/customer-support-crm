using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Notifications.Handlers;

public class TicketCreatedNotifyHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TicketCreatedNotifyHandler> _logger;

    public TicketCreatedNotifyHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<TicketCreatedNotifyHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Priority)
            .FirstOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);

        if (ticket is null || !ticket.AssignedToId.HasValue) return;

        var agent = await _context.Users.FindAsync([ticket.AssignedToId.Value], cancellationToken);
        if (agent is null) return;

        try
        {
            await _notificationService.SendAsync(
                notification.TenantId,
                "ticket.created",
                new NotificationRecipientInfo(agent.Id, RecipientType.Agent, agent.Email, agent.PhoneNumber),
                new Dictionary<string, string>
                {
                    ["ticketNumber"] = ticket.TicketNumber,
                    ["subject"] = ticket.Subject,
                    ["customerName"] = ticket.Customer?.Name ?? "",
                    ["priority"] = ticket.Priority?.Name ?? ""
                },
                System.Text.Json.JsonSerializer.Serialize(new { ticketId = ticket.Id }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket.created notification for {TicketId}", ticket.Id);
        }
    }
}
