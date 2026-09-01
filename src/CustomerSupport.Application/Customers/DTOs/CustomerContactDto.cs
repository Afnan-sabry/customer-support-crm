namespace CustomerSupport.Application.Customers.DTOs;

public record CustomerContactDto(Guid Id, string Name, string NameAr, string? Email, string? Phone, string? Title, bool IsPrimary);
