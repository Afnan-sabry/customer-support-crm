using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicies");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.FirstResponseMinutes).IsRequired();
        builder.Property(s => s.ResolutionMinutes).IsRequired();

        builder.HasOne(s => s.Priority).WithMany().HasForeignKey(s => s.PriorityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.Category).WithMany().HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => new { s.TenantId, s.PriorityId, s.CategoryId });
    }
}
