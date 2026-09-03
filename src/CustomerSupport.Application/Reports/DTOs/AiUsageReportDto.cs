namespace CustomerSupport.Application.Reports.DTOs;

public record AiUsageReportDto(
    List<AiSuggestionTypeBreakdown> SuggestionsByType,
    double AvgConfidence,
    int TotalTokensUsed,
    List<AiUsageTimeSeriesPoint> TimeSeries);

public record AiSuggestionTypeBreakdown(
    string Type, int TotalCount, int AcceptedCount, int RejectedCount,
    int PendingCount, double AcceptanceRate);

public record AiUsageTimeSeriesPoint(string Period, int SuggestionCount, double AcceptanceRate);
