using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public Guid TenantId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public Tenant? Tenant { get; set; }
}
