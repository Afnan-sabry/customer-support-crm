using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class PortalUserConfiguration : IEntityTypeConfiguration<PortalUser>
{
    public void Configure(EntityTypeBuilder<PortalUser> builder)
    {
        builder.ToTable("PortalUsers");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(256);
        builder.Property(p => p.PasswordHash).IsRequired();
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.FullNameAr).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(50);

        builder.HasIndex(p => new { p.TenantId, p.Email }).IsUnique();

        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Tenant).WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => p.IsActive);
    }
}
