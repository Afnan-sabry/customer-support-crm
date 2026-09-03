namespace CustomerSupport.Application.Assignment.DTOs;

public record AssignmentRuleDto(
    Guid Id, string Name, string NameAr,
    Guid? CategoryId, string? CategoryName,
    Guid? PriorityId, string? PriorityName,
    string Strategy, string? AgentPool,
    int Order, bool IsActive);
