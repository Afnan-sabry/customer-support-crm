namespace CustomerSupport.Application.Sla.DTOs;

public record TicketSlaDto(
    Guid Id, Guid TicketId, string TicketNumber,
    Guid SlaPolicyId, string SlaPolicyName,
    DateTime FirstResponseDue, DateTime ResolutionDue,
    DateTime? FirstRespondedAt, DateTime? ResolvedAt,
    bool FirstResponseBreached, bool ResolutionBreached);
