using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public TenantType Type { get; set; }
    public Guid? ParentTenantId { get; set; }
    public Tenant? ParentTenant { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public ICollection<Tenant> Children { get; set; } = new List<Tenant>();
}
