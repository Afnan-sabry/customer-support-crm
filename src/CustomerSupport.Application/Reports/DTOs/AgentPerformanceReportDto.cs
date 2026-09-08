namespace CustomerSupport.Application.Reports.DTOs;

public record AgentPerformanceReportDto(
    List<AgentPerformanceItem> Agents,
    AgentPerformanceTopPerformer? TopPerformer);

public record AgentPerformanceItem(
    Guid AgentId, string AgentName, int TicketsHandled, int TicketsResolved,
    double AvgResolutionMinutes, double AvgFirstResponseMinutes, double SlaCompliancePercent);

public record AgentPerformanceTopPerformer(Guid AgentId, string AgentName);
