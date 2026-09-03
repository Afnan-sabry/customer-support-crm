using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class WhatsAppChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly IDateTimeService _dateTimeService;

    public WhatsAppChannelProvider(AppDbContext context, IWhatsAppClient whatsAppClient, IDateTimeService dateTimeService)
    {
        _context = context;
        _whatsAppClient = whatsAppClient;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.WhatsApp;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var customer = await _context.Customers.FindAsync(conversation.CustomerId);
        var phone = customer?.Phone ?? conversation.ExternalReference ?? "";

        var externalId = await _whatsAppClient.SendTextMessageAsync(phone, content);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = ContentType.Text,
            Channel = ChannelType.WhatsApp,
            ExternalMessageId = externalId,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        return message;
    }
}
