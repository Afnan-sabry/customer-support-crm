namespace CustomerSupport.Application.Reports.DTOs;

public record TicketVolumeReportDto(
    List<TimeSeriesPoint> TimeSeries,
    List<CategoryBreakdownItem> CategoryBreakdown,
    List<PriorityBreakdownItem> PriorityBreakdown,
    int TotalCreated,
    int TotalResolved);

public record TimeSeriesPoint(string Period, int CreatedCount, int ResolvedCount);
public record CategoryBreakdownItem(string CategoryName, string CategoryNameAr, int Count);
public record PriorityBreakdownItem(string PriorityName, string PriorityNameAr, int Count);
