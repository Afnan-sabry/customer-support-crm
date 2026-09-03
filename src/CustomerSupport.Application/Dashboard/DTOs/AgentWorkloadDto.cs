namespace CustomerSupport.Application.Dashboard.DTOs;

public record AgentWorkloadDto(Guid AgentId, string AgentName, int OpenTickets, int OverdueTickets);
