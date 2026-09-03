using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Conversations.Queries;

public record GetConversationByIdQuery(Guid Id, int MessagePage = 1, int MessagePageSize = 50) : IRequest<ConversationDetailDto?>;

public record ConversationDetailDto(
    ConversationDto Conversation, List<MessageDto> Messages, int TotalMessages);

public class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ConversationDetailDto?>
{
    private readonly IConversationRepository _repository;
    private readonly Infrastructure.Persistence.AppDbContext _context;

    public GetConversationByIdQueryHandler(IConversationRepository repository, Infrastructure.Persistence.AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<ConversationDetailDto?> Handle(GetConversationByIdQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdWithMessagesAsync(
            request.Id, request.MessagePage, request.MessagePageSize, cancellationToken);

        if (conversation is null) return null;

        var totalMessages = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(_context.Messages.Where(m => m.ConversationId == request.Id), cancellationToken);

        var conversationDto = new ConversationDto(
            conversation.Id, conversation.CustomerId, conversation.Customer?.Name ?? "",
            conversation.TicketId, conversation.Ticket?.TicketNumber,
            conversation.Channel, conversation.Status,
            conversation.Subject, conversation.AssignedAgentId,
            conversation.AssignedAgent?.FullName,
            totalMessages, conversation.CreatedAt, conversation.ClosedAt);

        var messages = conversation.Messages.Select(m => new MessageDto(
            m.Id, m.ConversationId,
            m.Direction, m.SenderType,
            m.SenderId, null,
            m.Content, m.ContentType,
            m.Channel, m.Metadata,
            m.SentAt, m.DeliveredAt, m.ReadAt,
            m.Attachments.Select(a => new MessageAttachmentDto(
                a.Id, a.FileName, a.ContentType, a.FileSizeBytes)).ToList()
        )).ToList();

        return new ConversationDetailDto(conversationDto, messages, totalMessages);
    }
}
