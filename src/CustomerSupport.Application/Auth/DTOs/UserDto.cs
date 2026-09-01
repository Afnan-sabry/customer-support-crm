namespace CustomerSupport.Application.Auth.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string FullNameAr,
    Guid TenantId,
    string PreferredLanguage,
    IReadOnlyList<string> Roles);
