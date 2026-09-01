namespace CustomerSupport.Application.AuditLogs.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string EntityType,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    DateTime Timestamp,
    string? IpAddress);
