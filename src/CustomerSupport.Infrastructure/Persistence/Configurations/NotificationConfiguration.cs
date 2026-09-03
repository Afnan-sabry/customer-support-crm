using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.TemplateKey).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(500);
        builder.Property(n => n.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.BodyAr).IsRequired();

        builder.HasIndex(n => new { n.RecipientId, n.IsRead });
        builder.HasIndex(n => new { n.TenantId, n.RecipientId, n.CreatedAt });
    }
}
