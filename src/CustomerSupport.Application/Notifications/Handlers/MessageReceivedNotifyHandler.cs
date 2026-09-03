using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Notifications.Handlers;

public class MessageReceivedNotifyHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MessageReceivedNotifyHandler> _logger;

    public MessageReceivedNotifyHandler(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<MessageReceivedNotifyHandler> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Direction != MessageDirection.Inbound) return;

        var conversation = await _context.Conversations
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == notification.ConversationId, cancellationToken);

        if (conversation is null || !conversation.AssignedAgentId.HasValue) return;

        var agent = await _context.Users.FindAsync([conversation.AssignedAgentId.Value], cancellationToken);
        if (agent is null) return;

        try
        {
            await _notificationService.SendAsync(
                notification.TenantId,
                "conversation.new_message",
                new NotificationRecipientInfo(agent.Id, RecipientType.Agent, agent.Email, agent.PhoneNumber),
                new Dictionary<string, string>
                {
                    ["customerName"] = conversation.Customer?.Name ?? "",
                    ["channel"] = conversation.Channel.ToString()
                },
                System.Text.Json.JsonSerializer.Serialize(new { conversationId = conversation.Id }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send conversation.new_message notification for {ConversationId}", conversation.Id);
        }
    }
}
