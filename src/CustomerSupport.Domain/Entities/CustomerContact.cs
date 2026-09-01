namespace CustomerSupport.Domain.Entities;

public class CustomerContact : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }

    public Customer? Customer { get; set; }
}
