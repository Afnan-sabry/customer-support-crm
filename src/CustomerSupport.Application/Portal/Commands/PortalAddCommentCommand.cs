using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalAddCommentCommand(
    Guid TicketId, Guid CustomerId, Guid TenantId,
    string Content) : IRequest<PortalCommentDto>;

public class PortalAddCommentCommandHandler : IRequestHandler<PortalAddCommentCommand, PortalCommentDto>
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;

    public PortalAddCommentCommandHandler(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<PortalCommentDto> Handle(PortalAddCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Ticket not found");

        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.TicketId == request.TicketId, cancellationToken);

        if (conversation is not null)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ConversationId = conversation.Id,
                Direction = MessageDirection.Inbound,
                SenderType = SenderType.Customer,
                Content = request.Content,
                ContentType = ContentType.Text,
                Channel = conversation.Channel,
                SentAt = DateTime.UtcNow
            };
            await _context.Messages.AddAsync(message, cancellationToken);

            await _publisher.Publish(new MessageReceivedNotification(
                message.Id, conversation.Id, request.TenantId, MessageDirection.Inbound), cancellationToken);
        }

        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken);

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            UserId = null,
            Content = request.Content,
            IsInternal = false
        };
        await _context.TicketComments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new PortalCommentDto(comment.Id, comment.Content, customer?.Name ?? "", comment.CreatedAt, false);
    }
}
