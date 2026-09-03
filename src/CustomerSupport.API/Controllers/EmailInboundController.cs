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

public record InboundEmailDto(
    string From, string To, string Subject,
    string? HtmlBody, string? TextBody,
    string MessageId, string? InReplyTo);

[ApiController]
[Route("api/v1/webhooks/email")]
public class EmailInboundController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConversationRepository _conversationRepository;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly IConfiguration _configuration;

    public EmailInboundController(
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
    public async Task<IActionResult> ReceiveEmail([FromBody] InboundEmailDto dto)
    {
        var webhookKey = Request.Headers["X-Webhook-Key"].FirstOrDefault();
        var expectedKey = _configuration["Webhooks:EmailKey"];
        if (string.IsNullOrEmpty(expectedKey) || webhookKey != expectedKey)
            return Unauthorized();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == dto.From);

        Guid tenantId;
        if (customer is null)
        {
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = dto.From.Split('@')[0],
                NameAr = dto.From.Split('@')[0],
                Email = dto.From
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            tenantId = customer.TenantId;
        }

        Conversation? conversation = null;
        if (!string.IsNullOrEmpty(dto.InReplyTo))
        {
            conversation = await _conversationRepository.GetByExternalReferenceAsync(dto.InReplyTo);
        }

        var isNew = conversation is null;
        if (isNew)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customer.Id,
                Channel = ChannelType.Email,
                Status = ConversationStatus.Active,
                Subject = dto.Subject,
                ExternalReference = dto.MessageId
            };
            await _conversationRepository.AddAsync(conversation);
        }

        var metadata = JsonSerializer.Serialize(new
        {
            From = dto.From,
            To = dto.To,
            dto.Subject,
            dto.MessageId,
            dto.InReplyTo
        });

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation!.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = dto.HtmlBody ?? dto.TextBody ?? "",
            ContentType = dto.HtmlBody is not null ? ContentType.Html : ContentType.Text,
            Channel = ChannelType.Email,
            ExternalMessageId = dto.MessageId,
            Metadata = metadata,
            SentAt = _dateTimeService.UtcNow
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        if (isNew)
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, tenantId, ChannelType.Email));
        }

        await _publisher.Publish(new MessageReceivedNotification(
            message.Id, conversation.Id, tenantId, MessageDirection.Inbound));

        return Ok(new { conversationId = conversation.Id, messageId = message.Id });
    }
}
