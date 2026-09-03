namespace CustomerSupport.Application.Dashboard.DTOs;

public record DashboardStatsDto(
    int OpenTickets, int OverdueTickets, int ResolvedToday,
    int UnassignedTickets, int MyOpenTickets, int MyOverdueTickets);
