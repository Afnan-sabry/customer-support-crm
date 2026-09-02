using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class AssignmentRuleConfiguration : IEntityTypeConfiguration<AssignmentRule>
{
    public void Configure(EntityTypeBuilder<AssignmentRule> builder)
    {
        builder.ToTable("AssignmentRules");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Strategy).IsRequired().HasMaxLength(50);
        builder.Property(a => a.AgentPool).HasMaxLength(4000);

        builder.HasOne(a => a.Category).WithMany().HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Priority).WithMany().HasForeignKey(a => a.PriorityId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.TenantId, a.Order });
    }
}
