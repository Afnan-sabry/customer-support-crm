namespace CustomerSupport.Application.Customers.DTOs;

public record CustomerDto(Guid Id, string Name, string NameAr, string? Email, string? Phone, string? Company, string? CompanyAr, bool IsActive);
