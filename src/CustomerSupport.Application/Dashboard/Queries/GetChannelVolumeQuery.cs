using CustomerSupport.Application.Dashboard.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Dashboard.Queries;

public record GetChannelVolumeQuery(int Days = 30) : IRequest<List<ChannelVolumeDto>>;

public class GetChannelVolumeQueryHandler : IRequestHandler<GetChannelVolumeQuery, List<ChannelVolumeDto>>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetChannelVolumeQueryHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<ChannelVolumeDto>> Handle(GetChannelVolumeQuery request, CancellationToken cancellationToken)
    {
        var since = _dateTimeService.UtcNow.Date.AddDays(-request.Days);

        // EF Core cannot translate `.ToString()` on enums or `.Date.ToString()` inside a GroupBy
        // key expression, so materialize the raw rows first and group in memory.
        var conversations = await _context.Conversations
            .Where(c => c.CreatedAt >= since)
            .Select(c => new { c.Channel, c.CreatedAt })
            .ToListAsync(cancellationToken);

        return conversations
            .GroupBy(c => new { Channel = c.Channel.ToString(), Date = c.CreatedAt.Date.ToString("yyyy-MM-dd") })
            .Select(g => new ChannelVolumeDto(g.Key.Channel, g.Count(), g.Key.Date))
            .ToList();
    }
}
