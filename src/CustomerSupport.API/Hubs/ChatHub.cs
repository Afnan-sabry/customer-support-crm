using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.API.Hubs;

[Authorize(AuthenticationSchemes = "Bearer,Portal")]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPublisher _publisher;

    public ChatHub(AppDbContext context, IDateTimeService dateTimeService, IPublisher publisher)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _publisher = publisher;
    }

    public async Task JoinChat(Guid conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null) return;

        var customerIdClaim = Context.User?.FindFirst("CustomerId")?.Value;
        if (customerIdClaim is not null)
        {
            if (!Guid.TryParse(customerIdClaim, out var customerId) || conversation.CustomerId != customerId)
                return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{conversationId}");
    }

    public async Task SendMessage(Guid conversationId, string content)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null) return;

        var userId = GetUserId();
        var isAgent = Context.User?.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role) ?? false;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversationId,
            Direction = isAgent ? MessageDirection.Outbound : MessageDirection.Inbound,
            SenderType = isAgent ? SenderType.Agent : SenderType.Customer,
            SenderId = userId != Guid.Empty ? userId : null,
            Content = content,
            ContentType = ContentType.Text,
            Channel = ChannelType.LiveChat,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        await Clients.Group($"chat-{conversationId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.ConversationId,
            message.Direction,
            message.SenderType,
            message.SenderId,
            message.Content,
            message.SentAt
        });

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversationId, conversation.TenantId, message.Direction));
    }

    public async Task SendTypingIndicator(Guid conversationId)
    {
        await Clients.OthersInGroup($"chat-{conversationId}")
            .SendAsync("TypingIndicator", new { ConversationId = conversationId, UserId = GetUserId() });
    }

    public async Task EndChat(Guid conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation is null) return;

        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync();

        await Clients.Group($"chat-{conversationId}").SendAsync("ChatEnded", conversationId);
    }

    private Guid GetUserId()
    {
        var id = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("PortalUserId")?.Value;
        return id is not null ? Guid.Parse(id) : Guid.Empty;
    }
}
