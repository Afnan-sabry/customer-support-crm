using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Portal.Commands;

public record PortalSubmitTicketCommand(
    Guid CustomerId, Guid TenantId,
    Guid CategoryId, Guid PriorityId,
    string Subject, string Description) : IRequest<PortalTicketDto>;

public class PortalSubmitTicketCommandHandler : IRequestHandler<PortalSubmitTicketCommand, PortalTicketDto>
{
    private readonly AppDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<PortalSubmitTicketCommandHandler> _logger;

    public PortalSubmitTicketCommandHandler(
        AppDbContext context, ITicketRepository ticketRepository,
        IPublisher publisher, ILogger<PortalSubmitTicketCommandHandler> logger)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<PortalTicketDto> Handle(PortalSubmitTicketCommand request, CancellationToken cancellationToken)
    {
        var newStatus = await _context.TicketStatuses
            .FirstOrDefaultAsync(s => s.Name == "New", cancellationToken)
            ?? throw new KeyNotFoundException("Default ticket status 'New' not found");

        var ticketNumber = await _ticketRepository.GenerateTicketNumberAsync(cancellationToken);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            TicketNumber = ticketNumber,
            CustomerId = request.CustomerId,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            StatusId = newStatus.Id,
            Subject = request.Subject,
            Description = request.Description
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            CustomerId = request.CustomerId,
            TicketId = ticket.Id,
            Channel = ChannelType.Portal,
            Status = ConversationStatus.Active,
            Subject = request.Subject
        };
        await _context.Conversations.AddAsync(conversation, cancellationToken);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ConversationId = conversation.Id,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            Content = request.Description,
            ContentType = ContentType.Text,
            Channel = ChannelType.Portal,
            SentAt = DateTime.UtcNow
        };
        await _context.Messages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _publisher.Publish(new TicketCreatedNotification(
                ticket.Id, ticket.TenantId, ticket.PriorityId, ticket.CategoryId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish TicketCreatedNotification for portal ticket {TicketId}", ticket.Id);
        }

        var category = await _context.TicketCategories.FindAsync([request.CategoryId], cancellationToken);
        var priority = await _context.TicketPriorities.FindAsync([request.PriorityId], cancellationToken);

        return new PortalTicketDto(
            ticket.Id, ticket.TicketNumber, ticket.Subject,
            category?.Name ?? "", priority?.Name ?? "", newStatus.Name,
            ticket.CreatedAt, ticket.UpdatedAt);
    }
}
