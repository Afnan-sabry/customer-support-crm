namespace CustomerSupport.Application.Portal.DTOs;

public record PortalUserDto(Guid Id, string Email, string FullName, string FullNameAr, string? Phone, Guid CustomerId);
