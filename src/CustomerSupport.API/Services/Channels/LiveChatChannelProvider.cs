using CustomerSupport.API.Hubs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;

namespace CustomerSupport.API.Services.Channels;

// NOTE: This provider lives in the API project (rather than Infrastructure, where the
// other IChannelProvider implementations live) because it depends on IHubContext<ChatHub>,
// and ChatHub is defined in the API project. Infrastructure cannot reference API (API already
// references Infrastructure), so placing it here avoids a circular project reference while
// keeping the same IChannelProvider contract. It is registered directly in Program.cs.
public class LiveChatChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IDateTimeService _dateTimeService;

    public LiveChatChannelProvider(
        AppDbContext context,
        IHubContext<ChatHub> hubContext,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _hubContext = hubContext;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.LiveChat;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = contentType,
            Channel = ChannelType.LiveChat,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.Group($"chat-{conversation.Id}")
            .SendAsync("ReceiveMessage", new
            {
                message.Id,
                message.ConversationId,
                message.Direction,
                message.SenderType,
                message.SenderId,
                message.Content,
                message.ContentType,
                message.SentAt
            });

        return message;
    }
}
