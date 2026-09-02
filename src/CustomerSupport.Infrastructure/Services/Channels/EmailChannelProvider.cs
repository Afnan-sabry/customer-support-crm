using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CustomerSupport.Infrastructure.Services.Channels;

public class EmailChannelProvider : IChannelProvider
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeService _dateTimeService;

    public EmailChannelProvider(AppDbContext context, IEmailSender emailSender, IDateTimeService dateTimeService)
    {
        _context = context;
        _emailSender = emailSender;
        _dateTimeService = dateTimeService;
    }

    public ChannelType Channel => ChannelType.Email;

    public async Task<Message> SendMessageAsync(Conversation conversation, string content, ContentType contentType, Guid? agentId = null)
    {
        var customer = await _context.Customers.FindAsync(conversation.CustomerId);
        var messageId = $"<{Guid.NewGuid()}@crm.local>";

        var metadata = JsonSerializer.Serialize(new
        {
            From = "support@crm.local",
            To = customer?.Email ?? "",
            Subject = conversation.Subject ?? "",
            MessageId = messageId
        });

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conversation.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Outbound,
            SenderType = agentId.HasValue ? SenderType.Agent : SenderType.System,
            SenderId = agentId,
            Content = content,
            ContentType = contentType == ContentType.Text ? ContentType.Html : contentType,
            Channel = ChannelType.Email,
            ExternalMessageId = messageId,
            Metadata = metadata,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);

        if (string.IsNullOrEmpty(conversation.ExternalReference))
        {
            conversation.ExternalReference = messageId;
            _context.Conversations.Update(conversation);
        }

        await _context.SaveChangesAsync();

        await _emailSender.SendAsync(
            customer?.Email ?? "",
            conversation.Subject ?? "Support message",
            content,
            conversation.ExternalReference);

        return message;
    }
}
