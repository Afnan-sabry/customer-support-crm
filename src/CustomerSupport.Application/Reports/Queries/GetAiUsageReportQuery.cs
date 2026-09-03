using CustomerSupport.Application.Reports.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Reports.Queries;

public record GetAiUsageReportQuery(
    DateTime StartDate, DateTime EndDate) : IRequest<AiUsageReportDto>;

public class GetAiUsageReportQueryHandler : IRequestHandler<GetAiUsageReportQuery, AiUsageReportDto>
{
    private readonly AppDbContext _context;

    public GetAiUsageReportQueryHandler(AppDbContext context) => _context = context;

    public async Task<AiUsageReportDto> Handle(GetAiUsageReportQuery request, CancellationToken cancellationToken)
    {
        var suggestions = await _context.AiSuggestions
            .Where(s => s.CreatedAt >= request.StartDate && s.CreatedAt <= request.EndDate)
            .Select(s => new { s.Type, s.Status, s.Confidence, s.TokensUsed, s.CreatedAt })
            .ToListAsync(cancellationToken);

        var byType = suggestions
            .GroupBy(s => s.Type)
            .Select(g =>
            {
                var total = g.Count();
                var accepted = g.Count(s => s.Status == "Accepted" || s.Status == "AutoApplied");
                var rejected = g.Count(s => s.Status == "Rejected");
                var pending = g.Count(s => s.Status == "Pending");
                return new AiSuggestionTypeBreakdown(
                    g.Key, total, accepted, rejected, pending,
                    total > 0 ? Math.Round(accepted * 100.0 / total, 1) : 0);
            })
            .OrderByDescending(t => t.TotalCount)
            .ToList();

        var avgConfidence = suggestions.Where(s => s.Confidence.HasValue).ToList();
        var totalTokens = suggestions.Sum(s => s.TokensUsed);

        var timeSeries = suggestions
            .GroupBy(s => s.CreatedAt.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var total = g.Count();
                var accepted = g.Count(s => s.Status == "Accepted" || s.Status == "AutoApplied");
                return new AiUsageTimeSeriesPoint(
                    g.Key, total,
                    total > 0 ? Math.Round(accepted * 100.0 / total, 1) : 0);
            })
            .ToList();

        return new AiUsageReportDto(
            byType,
            avgConfidence.Count > 0 ? Math.Round((double)avgConfidence.Average(s => s.Confidence!.Value), 2) : 0,
            totalTokens,
            timeSeries);
    }
}
