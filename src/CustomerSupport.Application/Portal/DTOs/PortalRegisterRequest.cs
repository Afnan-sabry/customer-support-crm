namespace CustomerSupport.Application.Portal.DTOs;

public record PortalRegisterRequest(string Email, string Password, string FullName, string FullNameAr, string? Phone);
