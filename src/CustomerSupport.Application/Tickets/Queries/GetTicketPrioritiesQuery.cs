using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Queries;

public record GetTicketPrioritiesQuery : IRequest<List<TicketPriorityDto>>;

public class GetTicketPrioritiesQueryHandler : IRequestHandler<GetTicketPrioritiesQuery, List<TicketPriorityDto>>
{
    private readonly AppDbContext _context;

    public GetTicketPrioritiesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<TicketPriorityDto>> Handle(GetTicketPrioritiesQuery request, CancellationToken cancellationToken)
        => await _context.TicketPriorities.Where(p => p.IsActive)
            .OrderBy(p => p.Level)
            .Select(p => new TicketPriorityDto(p.Id, p.Name, p.NameAr, p.Level))
            .ToListAsync(cancellationToken);
}
