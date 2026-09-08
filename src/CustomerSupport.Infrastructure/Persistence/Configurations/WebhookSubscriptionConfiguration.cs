using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Url).IsRequired().HasMaxLength(2000);
        builder.Property(w => w.Secret).IsRequired().HasMaxLength(500);
        builder.Property(w => w.Events).IsRequired();
        builder.Property(w => w.Headers).HasMaxLength(4000);

        builder.HasIndex(w => new { w.TenantId, w.IsActive });
        builder.HasIndex(w => new { w.TenantId, w.Name }).IsUnique();

        builder.HasMany(w => w.DeliveryLogs).WithOne(d => d.Subscription)
            .HasForeignKey(d => d.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
