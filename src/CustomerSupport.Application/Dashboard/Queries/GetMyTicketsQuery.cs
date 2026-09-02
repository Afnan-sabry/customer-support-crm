using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetMyTicketsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedList<TicketDto>>;

public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, PaginatedList<TicketDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyTicketsQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<TicketDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var query = _context.Tickets
            .Where(t => t.AssignedToId == _currentUserService.UserId)
            .Where(t => !finalStatusIds.Contains(t.StatusId))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto(
                t.Id, t.TicketNumber, t.Subject,
                t.Customer!.Name, t.Category!.Name, t.Priority!.Name,
                t.Status!.Name, t.AssignedTo != null ? t.AssignedTo.FullName : null,
                t.CreatedAt));

        return await PaginatedList<TicketDto>.CreateAsync(query, request.Page, request.PageSize, cancellationToken);
    }
}
