namespace CustomerSupport.Application.Sla.DTOs;

public record SlaPolicyDto(
    Guid Id, string Name, string NameAr,
    Guid? PriorityId, string? PriorityName,
    Guid? CategoryId, string? CategoryName,
    int FirstResponseMinutes, int ResolutionMinutes, bool IsActive);
