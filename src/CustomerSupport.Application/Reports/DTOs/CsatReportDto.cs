namespace CustomerSupport.Application.Reports.DTOs;

public record CsatReportDto(
    double AverageRating,
    int TotalResponses,
    List<RatingDistributionItem> RatingDistribution,
    List<RecentFeedbackItem> RecentFeedback,
    List<CsatTimeSeriesPoint> TimeSeries);

public record RatingDistributionItem(int Rating, int Count, double Percentage);
public record RecentFeedbackItem(Guid TicketId, string TicketNumber, string CustomerName, int Rating, string? Comment, DateTime SubmittedAt);
public record CsatTimeSeriesPoint(string Period, double AverageRating, int ResponseCount);
