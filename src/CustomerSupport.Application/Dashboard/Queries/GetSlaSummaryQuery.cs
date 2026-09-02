using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetSlaSummaryQuery : IRequest<SlaSummaryDto>;

public class GetSlaSummaryQueryHandler : IRequestHandler<GetSlaSummaryQuery, SlaSummaryDto>
{
    private readonly AppDbContext _context;

    public GetSlaSummaryQueryHandler(AppDbContext context) => _context = context;

    public async Task<SlaSummaryDto> Handle(GetSlaSummaryQuery request, CancellationToken cancellationToken)
    {
        var totalTracked = await _context.TicketSlas.CountAsync(cancellationToken);
        if (totalTracked == 0)
            return new SlaSummaryDto(0, 0, 0, 0, 0, 100, 100);

        var frBreached = await _context.TicketSlas.CountAsync(ts => ts.FirstResponseBreached, cancellationToken);
        var frOnTime = await _context.TicketSlas.CountAsync(ts => !ts.FirstResponseBreached && ts.FirstRespondedAt.HasValue, cancellationToken);

        var resBreached = await _context.TicketSlas.CountAsync(ts => ts.ResolutionBreached, cancellationToken);
        var resOnTime = await _context.TicketSlas.CountAsync(ts => !ts.ResolutionBreached && ts.ResolvedAt.HasValue, cancellationToken);

        var frTotal = frOnTime + frBreached;
        var resTotal = resOnTime + resBreached;

        return new SlaSummaryDto(totalTracked,
            frOnTime, frBreached, resOnTime, resBreached,
            frTotal > 0 ? Math.Round(frOnTime * 100.0 / frTotal, 1) : 100,
            resTotal > 0 ? Math.Round(resOnTime * 100.0 / resTotal, 1) : 100);
    }
}
