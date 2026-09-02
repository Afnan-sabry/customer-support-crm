using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Conversations.Queries;

public record GetConversationsQuery(
    ChannelType? Channel, ConversationStatus? Status,
    Guid? CustomerId, Guid? AssignedAgentId,
    string? Search, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<ConversationDto>>;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PaginatedList<ConversationDto>>
{
    private readonly IConversationRepository _repository;

    public GetConversationsQueryHandler(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var query = _repository.GetQueryable();

        if (request.Channel.HasValue) query = query.Where(c => c.Channel == request.Channel.Value);
        if (request.Status.HasValue) query = query.Where(c => c.Status == request.Status.Value);
        if (request.CustomerId.HasValue) query = query.Where(c => c.CustomerId == request.CustomerId.Value);
        if (request.AssignedAgentId.HasValue) query = query.Where(c => c.AssignedAgentId == request.AssignedAgentId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(c =>
                (c.Subject != null && c.Subject.ToLower().Contains(search)) ||
                c.Customer!.Name.ToLower().Contains(search));
        }

        var projected = query.OrderByDescending(c => c.UpdatedAt).Select(c =>
            new ConversationDto(
                c.Id, c.CustomerId, c.Customer!.Name,
                c.TicketId, c.Ticket != null ? c.Ticket.TicketNumber : null,
                c.Channel, c.Status,
                c.Subject, c.AssignedAgentId,
                c.AssignedAgent != null ? c.AssignedAgent.FullName : null,
                c.Messages.Count, c.CreatedAt, c.ClosedAt));

        return await PaginatedList<ConversationDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
