namespace CustomerSupport.Application.Escalation.DTOs;

public record EscalationRuleDto(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, string? PriorityName,
    Guid? CategoryId, string? CategoryName,
    string TriggerType, int TriggerAfterMinutes,
    string ActionType, string? ActionTarget,
    int Order, bool IsActive);
