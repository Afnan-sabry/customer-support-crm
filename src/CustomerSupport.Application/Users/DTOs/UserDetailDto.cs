namespace CustomerSupport.Application.Users.DTOs;

public record UserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string FullNameAr,
    string? Phone,
    Guid TenantId,
    string PreferredLanguage,
    bool IsActive,
    IReadOnlyList<string> Roles);
