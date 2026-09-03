using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Conversations.Commands;

public record SendMessageCommand(
    Guid ConversationId, string Content,
    ContentType ContentType = ContentType.Text) : IRequest<MessageDto>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly AppDbContext _context;
    private readonly IChannelProviderFactory _channelProviderFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        AppDbContext context,
        IChannelProviderFactory channelProviderFactory,
        ICurrentUserService currentUserService,
        IPublisher publisher,
        IDateTimeService dateTimeService,
        ILogger<SendMessageCommandHandler> logger)
    {
        _context = context;
        _channelProviderFactory = channelProviderFactory;
        _currentUserService = currentUserService;
        _publisher = publisher;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conversation {request.ConversationId} not found");

        var provider = _channelProviderFactory.GetProvider(conversation.Channel);
        var message = await provider.SendMessageAsync(
            conversation, request.Content, request.ContentType, _currentUserService.UserId);

        try
        {
            await _publisher.Publish(new MessageReceivedNotification(
                message.Id, message.ConversationId, message.TenantId, message.Direction), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish MessageReceivedNotification for {MessageId}", message.Id);
        }

        var senderName = _currentUserService.UserId != Guid.Empty
            ? (await _context.Users.FindAsync([_currentUserService.UserId], cancellationToken))?.FullName
            : null;

        return new MessageDto(
            message.Id, message.ConversationId,
            message.Direction, message.SenderType,
            message.SenderId, senderName,
            message.Content, message.ContentType,
            message.Channel, message.Metadata,
            message.SentAt, message.DeliveredAt, message.ReadAt,
            []);
    }
}
