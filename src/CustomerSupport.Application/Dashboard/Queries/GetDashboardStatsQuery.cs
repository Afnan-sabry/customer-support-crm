using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public GetDashboardStatsQueryHandler(AppDbContext context, ICurrentUserService currentUserService, IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var finalStatusIds = await _context.TicketStatuses
            .Where(s => s.IsFinal).Select(s => s.Id).ToListAsync(cancellationToken);

        var today = _dateTimeService.UtcNow.Date;
        var userId = _currentUserService.UserId;

        var openTickets = await _context.Tickets
            .CountAsync(t => !finalStatusIds.Contains(t.StatusId), cancellationToken);

        var overdueTickets = await _context.TicketSlas
            .CountAsync(ts => (ts.FirstResponseBreached || ts.ResolutionBreached)
                && !finalStatusIds.Contains(ts.Ticket.StatusId), cancellationToken);

        var resolvedToday = await _context.Tickets
            .CountAsync(t => finalStatusIds.Contains(t.StatusId)
                && t.UpdatedAt >= today, cancellationToken);

        var unassignedTickets = await _context.Tickets
            .CountAsync(t => !t.AssignedToId.HasValue
                && !finalStatusIds.Contains(t.StatusId), cancellationToken);

        var myOpenTickets = await _context.Tickets
            .CountAsync(t => t.AssignedToId == userId
                && !finalStatusIds.Contains(t.StatusId), cancellationToken);

        var myOverdueTickets = await _context.TicketSlas
            .CountAsync(ts => ts.Ticket.AssignedToId == userId
                && (ts.FirstResponseBreached || ts.ResolutionBreached)
                && !finalStatusIds.Contains(ts.Ticket.StatusId), cancellationToken);

        return new DashboardStatsDto(openTickets, overdueTickets, resolvedToday,
            unassignedTickets, myOpenTickets, myOverdueTickets);
    }
}
