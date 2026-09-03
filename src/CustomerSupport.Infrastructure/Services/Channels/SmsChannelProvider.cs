using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class SmsChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly ISmsClient _smsClient;
    private readonly IDateTimeService _dateTimeService;

    public SmsChannelProvider(AppDbContext context, ISmsClient smsClient, IDateTimeService dateTimeService)
    {
        _context = context;
        _smsClient = smsClient;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.SMS;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var customer = await _context.Customers.FindAsync(conversation.CustomerId);
        var phone = customer?.Phone ?? conversation.ExternalReference ?? "";

        var externalId = await _smsClient.SendAsync(phone, content);

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
            Channel = ChannelType.SMS,
            ExternalMessageId = externalId,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        return message;
    }
}
