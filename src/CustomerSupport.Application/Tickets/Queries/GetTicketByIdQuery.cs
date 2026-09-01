using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Tickets.Queries;

public record GetTicketByIdQuery(Guid Id) : IRequest<TicketDetailDto?>;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketDetailDto?>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketDetailDto?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (ticket is null) return null;

        return new TicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId, ticket.Customer?.Name ?? "",
            ticket.CategoryId, ticket.Category?.Name ?? "", ticket.PriorityId, ticket.Priority?.Name ?? "",
            ticket.StatusId, ticket.Status?.Name ?? "", ticket.AssignedToId, ticket.AssignedTo?.FullName,
            ticket.Subject, ticket.Description, ticket.CreatedAt, ticket.UpdatedAt,
            ticket.Comments.Select(c => new TicketCommentDto(c.Id, c.UserId, c.User?.FullName ?? "", c.Content, c.IsInternal, c.CreatedAt)).ToList(),
            ticket.Attachments.Select(a => new TicketAttachmentDto(a.Id, a.FileName, a.ContentType, a.FileSize, a.CreatedAt)).ToList(),
            ticket.History.Select(h => new TicketHistoryDto(h.Id, h.User?.FullName, h.Field, h.OldValue, h.NewValue, h.CreatedAt)).ToList());
    }
}
