using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetRecentSlaBreachesQuery(int Count = 10) : IRequest<List<SlaBreachDto>>;

public class GetRecentSlaBreachesQueryHandler : IRequestHandler<GetRecentSlaBreachesQuery, List<SlaBreachDto>>
{
    private readonly AppDbContext _context;

    public GetRecentSlaBreachesQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<SlaBreachDto>> Handle(GetRecentSlaBreachesQuery request, CancellationToken cancellationToken)
    {
        return await _context.SlaBreachLogs
            .Include(b => b.Ticket)
            .Include(b => b.SlaPolicy)
            .OrderByDescending(b => b.BreachedAt)
            .Take(request.Count)
            .Select(b => new SlaBreachDto(
                b.TicketId, b.Ticket.TicketNumber, b.BreachType,
                b.SlaPolicy.Name, b.DueAt, b.BreachedAt,
                (b.BreachedAt - b.DueAt).TotalMinutes))
            .ToListAsync(cancellationToken);
    }
}
