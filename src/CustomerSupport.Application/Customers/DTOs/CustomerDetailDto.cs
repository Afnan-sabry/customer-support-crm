namespace CustomerSupport.Application.Customers.DTOs;

public record CustomerDetailDto(
    Guid Id, string Name, string NameAr, string? Email, string? Phone,
    string? Company, string? CompanyAr, string? Address, bool IsActive,
    List<CustomerContactDto> Contacts);
