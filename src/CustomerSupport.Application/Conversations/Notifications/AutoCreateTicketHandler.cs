using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Conversations.Notifications;

public class AutoCreateTicketHandler : INotificationHandler<ConversationCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<AutoCreateTicketHandler> _logger;

    public AutoCreateTicketHandler(
        AppDbContext context,
        ITicketRepository ticketRepository,
        IPublisher publisher,
        ILogger<AutoCreateTicketHandler> logger)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(ConversationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, cancellationToken);

        if (conversation is null || conversation.TicketId.HasValue) return;

        var defaultCategory = await _context.TicketCategories.FirstOrDefaultAsync(cancellationToken);
        var defaultPriority = await _context.TicketPriorities.FirstOrDefaultAsync(c => c.Name == "Medium", cancellationToken)
            ?? await _context.TicketPriorities.FirstOrDefaultAsync(cancellationToken);
        var newStatus = await _context.TicketStatuses.FirstOrDefaultAsync(s => s.Name == "New", cancellationToken);

        if (defaultCategory is null || defaultPriority is null || newStatus is null)
        {
            _logger.LogWarning("Cannot auto-create ticket: missing reference data");
            return;
        }

        var ticketNumber = await _ticketRepository.GenerateTicketNumberAsync(cancellationToken);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            TicketNumber = ticketNumber,
            CustomerId = conversation.CustomerId,
            CategoryId = defaultCategory.Id,
            PriorityId = defaultPriority.Id,
            StatusId = newStatus.Id,
            Subject = conversation.Subject ?? $"{notification.Channel} conversation",
            Description = $"Auto-created from {notification.Channel} conversation"
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        conversation.TicketId = ticket.Id;
        _context.Conversations.Update(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _publisher.Publish(new TicketCreatedNotification(
                ticket.Id, ticket.TenantId, ticket.PriorityId, ticket.CategoryId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish TicketCreatedNotification for auto-created ticket {TicketId}", ticket.Id);
        }
    }
}
