namespace CustomerSupport.Application.Reports.DTOs;

public record SlaPerformanceReportDto(
    double OverallFirstResponseCompliance,
    double OverallResolutionCompliance,
    List<SlaTimeSeriesPoint> TimeSeries,
    List<SlaBreachDetailItem> BreachDetails);

public record SlaTimeSeriesPoint(string Period, int FirstResponseOnTime, int FirstResponseBreached, int ResolutionOnTime, int ResolutionBreached);
public record SlaBreachDetailItem(Guid TicketId, string TicketNumber, string BreachType, string PolicyName, DateTime DueAt, DateTime BreachedAt, double MinutesLate);
