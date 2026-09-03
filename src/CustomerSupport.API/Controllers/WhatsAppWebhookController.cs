using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CustomerSupport.API.Controllers;

public record WhatsAppInboundDto(
    string From, string MessageType,
    string? Text, string? MediaUrl, string? Caption,
    string MessageId);

[ApiController]
[Route("api/v1/webhooks/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;

    public WhatsAppWebhookController(
        AppDbContext context,
        IConversationRepository conversationRepository,
        IPublisher publisher,
        IDateTimeService dateTimeService,
        IConfiguration configuration)
    {
        _context = context;
        _conversationRepository = conversationRepository;
        _publisher = publisher;
        _dateTimeService = dateTimeService;
        _configuration = configuration;
    }

    [HttpPost("inbound")]
    public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppInboundDto dto)
    {
        var webhookKey = Request.Headers["X-Webhook-Key"].FirstOrDefault();
        var expectedKey = _configuration["Webhooks:WhatsAppKey"];
        if (string.IsNullOrEmpty(expectedKey) || webhookKey != expectedKey)
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == dto.From);

        Guid tenantId;
        if (customer is null)
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.From,
                NameAr = dto.From,
                Phone = dto.From
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            tenantId = customer.TenantId;
        }

        var conversation = await _conversationRepository.GetByExternalReferenceAsync(dto.From);

        var isNew = conversation is null;
        if (isNew)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customer.Id,
                Channel = ChannelType.WhatsApp,
                Status = ConversationStatus.Active,
                ExternalReference = dto.From
            };
            await _conversationRepository.AddAsync(conversation);
        }

        var metadata = dto.MediaUrl is not null
            ? JsonSerializer.Serialize(new { dto.MediaUrl, dto.Caption })
            : null;

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation!.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = dto.Text ?? dto.Caption ?? "",
            ContentType = ContentType.Text,
            Channel = ChannelType.WhatsApp,
            ExternalMessageId = dto.MessageId,
            Metadata = metadata,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, tenantId, ChannelType.WhatsApp));
        }

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversation.Id, tenantId, MessageDirection.Inbound));

        return Ok(new { conversationId = conversation.Id, messageId = message.Id });
    }
}
