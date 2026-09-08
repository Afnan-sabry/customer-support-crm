using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Handlers;

public class WebhookConversationCreatedHandler : INotificationHandler<ConversationCreatedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;
    private readonly AppDbContext _context;

    public WebhookConversationCreatedHandler(IWebhookDispatcher dispatcher, AppDbContext context)
    {
        _dispatcher = dispatcher;
        _context = context;
    }

    public async Task Handle(ConversationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, cancellationToken);
        if (conversation is null) return;

        await _dispatcher.DispatchAsync(conversation.TenantId, "conversation.created", new
        {
            conversationId = conversation.Id,
            channel = conversation.Channel.ToString(),
            customerName = conversation.Customer?.Name
        });
    }
}

public class WebhookConversationClosedHandler : INotificationHandler<ConversationClosedNotification>
{
    private readonly IWebhookDispatcher _dispatcher;

    public WebhookConversationClosedHandler(IWebhookDispatcher dispatcher) => _dispatcher = dispatcher;

    public async Task Handle(ConversationClosedNotification notification, CancellationToken cancellationToken)
    {
        await _dispatcher.DispatchAsync(notification.TenantId, "conversation.closed", new
        {
            conversationId = notification.ConversationId,
            closedAt = DateTime.UtcNow
        });
    }
}
