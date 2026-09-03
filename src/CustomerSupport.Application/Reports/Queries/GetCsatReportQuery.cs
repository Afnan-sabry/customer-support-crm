using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetCsatReportQuery(
    DateTime StartDate, DateTime EndDate,
    Guid? CategoryId = null) : IRequest<CsatReportDto>;

public class GetCsatReportQueryHandler : IRequestHandler<GetCsatReportQuery, CsatReportDto>
{
    private readonly AppDbContext _context;

    public GetCsatReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<CsatReportDto> Handle(GetCsatReportQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TicketFeedbacks
            .Include(f => f.Ticket)
            .Include(f => f.Customer)
            .Where(f => f.SubmittedAt >= request.StartDate && f.SubmittedAt <= request.EndDate);

        if (request.CategoryId.HasValue)
            query = query.Where(f => f.Ticket.CategoryId == request.CategoryId.Value);

        var feedbacks = await query.Select(f => new
        {
            f.TicketId,
            f.Ticket.TicketNumber,
            CustomerName = f.Customer.Name,
            f.Rating,
            f.Comment,
            f.SubmittedAt
        }).ToListAsync(cancellationToken);

        if (feedbacks.Count == 0)
            return new CsatReportDto(0, 0, [], [], []);

        var avgRating = Math.Round(feedbacks.Average(f => f.Rating), 2);

        var distribution = Enumerable.Range(1, 5)
            .Select(rating =>
            {
                var count = feedbacks.Count(f => f.Rating == rating);
                return new RatingDistributionItem(rating, count,
                    Math.Round(count * 100.0 / feedbacks.Count, 1));
            })
            .ToList();

        var recent = feedbacks
            .OrderByDescending(f => f.SubmittedAt)
            .Take(20)
            .Select(f => new RecentFeedbackItem(
                f.TicketId, f.TicketNumber, f.CustomerName,
                f.Rating, f.Comment, f.SubmittedAt))
            .ToList();

        var timeSeries = feedbacks
            .GroupBy(f => f.SubmittedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new CsatTimeSeriesPoint(
                g.Key, Math.Round(g.Average(f => f.Rating), 2), g.Count()))
            .ToList();

        return new CsatReportDto(avgRating, feedbacks.Count, distribution, recent, timeSeries);
    }
}
