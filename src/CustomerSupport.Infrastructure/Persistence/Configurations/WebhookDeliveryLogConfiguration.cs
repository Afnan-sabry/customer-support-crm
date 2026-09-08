using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("WebhookDeliveryLogs");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Event).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Payload).IsRequired().HasMaxLength(65536);
        builder.Property(d => d.ResponseBody).HasMaxLength(4000);
        builder.Property(d => d.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(d => new { d.SubscriptionId, d.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(d => new { d.TenantId, d.Event, d.CreatedAt }).IsDescending(false, false, true);
    }
}
