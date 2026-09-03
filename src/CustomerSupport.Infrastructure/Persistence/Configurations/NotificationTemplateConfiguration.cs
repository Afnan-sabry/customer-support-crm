using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Key).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Subject).IsRequired().HasMaxLength(500);
        builder.Property(n => n.SubjectAr).IsRequired().HasMaxLength(500);
        builder.Property(n => n.BodyTemplate).IsRequired();
        builder.Property(n => n.BodyTemplateAr).IsRequired();

        builder.HasIndex(n => new { n.TenantId, n.Key }).IsUnique();
    }
}
