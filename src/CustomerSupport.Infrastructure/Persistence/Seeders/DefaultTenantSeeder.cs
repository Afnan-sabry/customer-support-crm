using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Persistence.Seeders;

public static class DefaultTenantSeeder
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext context)
    {
        if (!await context.Tenants.AnyAsync(t => t.Id == DefaultTenantId))
        {
            context.Tenants.Add(new Tenant
            {
                Id = DefaultTenantId,
                Name = "Default Organization",
                NameAr = "المنظمة الافتراضية",
                Type = TenantType.Branch,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
