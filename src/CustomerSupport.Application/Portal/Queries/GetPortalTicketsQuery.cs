using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Portal.Queries;

public record GetPortalTicketsQuery(
    Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<PortalTicketDto>>;

public class GetPortalTicketsQueryHandler : IRequestHandler<GetPortalTicketsQuery, PaginatedList<PortalTicketDto>>
{
    private readonly AppDbContext _context;

    public GetPortalTicketsQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<PortalTicketDto>> Handle(GetPortalTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tickets
            .Where(t => t.CustomerId == request.CustomerId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PortalTicketDto(
                t.Id, t.TicketNumber, t.Subject,
                t.Category!.Name, t.Priority!.Name, t.Status!.Name,
                t.CreatedAt, t.UpdatedAt));

        return await PaginatedList<PortalTicketDto>.CreateAsync(query, request.Page, request.PageSize, cancellationToken);
    }
}
