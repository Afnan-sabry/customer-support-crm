namespace CustomerSupport.Application.Tickets.DTOs;

public record TicketHistoryDto(Guid Id, string? UserName, string Field, string? OldValue, string? NewValue, DateTime CreatedAt);
