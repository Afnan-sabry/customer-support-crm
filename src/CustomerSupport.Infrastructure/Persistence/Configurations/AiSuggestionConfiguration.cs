using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class AiSuggestionConfiguration : IEntityTypeConfiguration<AiSuggestion>
{
    public void Configure(EntityTypeBuilder<AiSuggestion> builder)
    {
        builder.ToTable("AiSuggestions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Input).IsRequired();
        builder.Property(a => a.Output).IsRequired();
        builder.Property(a => a.Confidence).HasPrecision(5, 4);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Model).IsRequired().HasMaxLength(100);

        builder.HasOne(a => a.Ticket).WithMany().HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TenantId, a.TicketId });
        builder.HasIndex(a => new { a.TenantId, a.Type, a.Status });
    }
}
