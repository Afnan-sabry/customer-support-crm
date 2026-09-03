using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetChannelAnalyticsReportQuery(
    DateTime StartDate, DateTime EndDate) : IRequest<ChannelAnalyticsReportDto>;

public class GetChannelAnalyticsReportQueryHandler : IRequestHandler<GetChannelAnalyticsReportQuery, ChannelAnalyticsReportDto>
{
    private readonly AppDbContext _context;

    public GetChannelAnalyticsReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<ChannelAnalyticsReportDto> Handle(GetChannelAnalyticsReportQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _context.Conversations
            .Where(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate)
            .Select(c => new { c.Id, c.Channel, c.CreatedAt })
            .ToListAsync(cancellationToken);

        var messages = await _context.Messages
            .Where(m => m.CreatedAt >= request.StartDate && m.CreatedAt <= request.EndDate)
            .Select(m => new { m.ConversationId, m.Channel, m.Direction, m.SentAt, m.CreatedAt })
            .ToListAsync(cancellationToken);

        var channelBreakdown = conversations
            .GroupBy(c => c.Channel.ToString())
            .Select(g =>
            {
                var convIds = g.Select(c => c.Id).ToHashSet();
                var channelMessages = messages.Where(m => convIds.Contains(m.ConversationId)).ToList();
                var inbound = channelMessages.Where(m => m.Direction == Domain.Enums.MessageDirection.Inbound).OrderBy(m => m.SentAt).ToList();
                var outbound = channelMessages.Where(m => m.Direction == Domain.Enums.MessageDirection.Outbound).OrderBy(m => m.SentAt).ToList();

                double avgResponse = 0;
                if (inbound.Count > 0 && outbound.Count > 0)
                {
                    var responseTimes = inbound
                        .Select(i => outbound.FirstOrDefault(o => o.ConversationId == i.ConversationId && o.SentAt > i.SentAt))
                        .Where(o => o is not null)
                        .Select(o => (o!.SentAt - inbound.First(i => i.ConversationId == o.ConversationId && i.SentAt < o.SentAt).SentAt).TotalMinutes)
                        .ToList();
                    if (responseTimes.Count > 0) avgResponse = Math.Round(responseTimes.Average(), 1);
                }

                return new ChannelBreakdownItem(g.Key, g.Count(), channelMessages.Count, avgResponse);
            })
            .OrderByDescending(c => c.ConversationCount)
            .ToList();

        var timeSeries = conversations
            .GroupBy(c => new { Period = c.CreatedAt.ToString("yyyy-MM-dd"), Channel = c.Channel.ToString() })
            .OrderBy(g => g.Key.Period)
            .Select(g => new ChannelTimeSeriesPoint(g.Key.Period, g.Key.Channel, g.Count()))
            .ToList();

        return new ChannelAnalyticsReportDto(channelBreakdown, timeSeries);
    }
}
