using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.FullNameAr).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.PreferredLanguage).IsRequired().HasMaxLength(5).HasDefaultValue("en");
        builder.Property(u => u.RefreshToken).HasMaxLength(500);

        builder.HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // No global query filter here: ApplicationUser must be resolvable during
        // anonymous auth flows (e.g. login), where ICurrentUserService.TenantId is
        // Guid.Empty. Tenant isolation for user queries is enforced explicitly in
        // Application-layer query handlers instead (see AppDbContext.OnModelCreating).
    }
}
