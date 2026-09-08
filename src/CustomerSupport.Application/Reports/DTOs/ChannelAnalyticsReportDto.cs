namespace CustomerSupport.Application.Reports.DTOs;

public record ChannelAnalyticsReportDto(
    List<ChannelBreakdownItem> ChannelBreakdown,
    List<ChannelTimeSeriesPoint> TimeSeries);

public record ChannelBreakdownItem(string Channel, int ConversationCount, int MessageCount, double AvgResponseMinutes);
public record ChannelTimeSeriesPoint(string Period, string Channel, int ConversationCount);
