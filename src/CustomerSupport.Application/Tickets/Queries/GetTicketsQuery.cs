using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Tickets.Queries;

public record GetTicketsQuery(
    Guid? StatusId, Guid? PriorityId, Guid? CategoryId, Guid? AssignedToId,
    string? Search, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<TicketDto>>;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, PaginatedList<TicketDto>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<PaginatedList<TicketDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = _ticketRepository.GetQueryable();

        if (request.StatusId.HasValue) query = query.Where(t => t.StatusId == request.StatusId.Value);
        if (request.PriorityId.HasValue) query = query.Where(t => t.PriorityId == request.PriorityId.Value);
        if (request.CategoryId.HasValue) query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        if (request.AssignedToId.HasValue) query = query.Where(t => t.AssignedToId == request.AssignedToId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(t =>
                t.TicketNumber.ToLower().Contains(search) ||
                t.Subject.ToLower().Contains(search));
        }

        var projected = query.OrderByDescending(t => t.CreatedAt).Select(t =>
            new TicketDto(t.Id, t.TicketNumber, t.Subject,
                t.Customer!.Name, t.Category!.Name, t.Priority!.Name, t.Status!.Name,
                t.AssignedTo != null ? t.AssignedTo.FullName : null, t.CreatedAt));

        return await PaginatedList<TicketDto>.CreateAsync(projected, request.Page, request.PageSize, cancellationToken);
    }
}
