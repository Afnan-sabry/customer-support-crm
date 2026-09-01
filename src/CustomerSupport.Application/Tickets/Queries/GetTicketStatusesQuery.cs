using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Queries;

public record GetTicketStatusesQuery : IRequest<List<TicketStatusDto>>;

public class GetTicketStatusesQueryHandler : IRequestHandler<GetTicketStatusesQuery, List<TicketStatusDto>>
{
    private readonly AppDbContext _context;

    public GetTicketStatusesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<TicketStatusDto>> Handle(GetTicketStatusesQuery request, CancellationToken cancellationToken)
        => await _context.TicketStatuses.Where(s => s.IsActive)
            .OrderBy(s => s.Order)
            .Select(s => new TicketStatusDto(s.Id, s.Name, s.NameAr, s.Order, s.IsFinal))
            .ToListAsync(cancellationToken);
}
