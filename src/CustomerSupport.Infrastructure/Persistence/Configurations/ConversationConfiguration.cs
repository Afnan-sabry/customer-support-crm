using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Subject).HasMaxLength(500);
        builder.Property(c => c.ExternalReference).HasMaxLength(500);

        builder.HasIndex(c => new { c.TenantId, c.CustomerId });
        builder.HasIndex(c => new { c.TenantId, c.Channel, c.Status });
        builder.HasIndex(c => c.ExternalReference);

        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Ticket).WithMany().HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(c => c.AssignedAgent).WithMany().HasForeignKey(c => c.AssignedAgentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(c => c.Messages).WithOne(m => m.Conversation).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}
