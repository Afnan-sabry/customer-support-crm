using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Conversations.Commands;

public record CreateConversationCommand(
    Guid CustomerId, ChannelType Channel,
    string? Subject, string? ExternalReference) : IRequest<ConversationDto>;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    private readonly IConversationRepository _repository;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreateConversationCommandHandler> _logger;

    public CreateConversationCommandHandler(
        IConversationRepository repository,
        AppDbContext context,
        ICurrentUserService currentUserService,
        IPublisher publisher,
        ILogger<CreateConversationCommandHandler> logger)
    {
        _repository = repository;
        _context = context;
        _currentUserService = currentUserService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ConversationDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken)
            ?? throw new KeyNotFoundException($"Customer {request.CustomerId} not found");

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            CustomerId = request.CustomerId,
            Channel = request.Channel,
            Status = ConversationStatus.Active,
            Subject = request.Subject,
            ExternalReference = request.ExternalReference,
            AssignedAgentId = _currentUserService.UserId != Guid.Empty ? _currentUserService.UserId : null
        };

        await _repository.AddAsync(conversation, cancellationToken);

        try
        {
            await _publisher.Publish(new ConversationCreatedNotification(
                conversation.Id, conversation.TenantId, conversation.Channel), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish ConversationCreatedNotification for {ConversationId}", conversation.Id);
        }

        return new ConversationDto(
            conversation.Id, conversation.CustomerId, customer.Name,
            conversation.TicketId, null,
            conversation.Channel, conversation.Status,
            conversation.Subject, conversation.AssignedAgentId, null,
            0, conversation.CreatedAt, null);
    }
}
