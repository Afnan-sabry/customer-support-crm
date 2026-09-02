using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class EscalationRuleConfiguration : IEntityTypeConfiguration<EscalationRule>
{
    public void Configure(EntityTypeBuilder<EscalationRule> builder)
    {
        builder.ToTable("EscalationRules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TriggerType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ActionTarget).HasMaxLength(500);

        builder.HasOne(e => e.Priority).WithMany().HasForeignKey(e => e.PriorityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.TenantId, e.Order });
    }
}
