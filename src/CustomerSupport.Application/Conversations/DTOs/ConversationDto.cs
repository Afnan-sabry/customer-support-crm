using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Conversations.DTOs;

public record ConversationDto(
    Guid Id, Guid CustomerId, string CustomerName,
    Guid? TicketId, string? TicketNumber,
    ChannelType Channel, ConversationStatus Status,
    string? Subject, Guid? AssignedAgentId, string? AssignedAgentName,
    int MessageCount, DateTime CreatedAt, DateTime? ClosedAt);
