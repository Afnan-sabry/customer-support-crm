using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class TicketFeedbackConfiguration : IEntityTypeConfiguration<TicketFeedback>
{
    public void Configure(EntityTypeBuilder<TicketFeedback> builder)
    {
        builder.ToTable("TicketFeedbacks");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Rating).IsRequired();
        builder.Property(f => f.Comment).HasMaxLength(1000);
        builder.Property(f => f.SubmittedAt).IsRequired();

        builder.HasOne(f => f.Ticket).WithMany().HasForeignKey(f => f.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Customer).WithMany().HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.TenantId, f.TicketId }).IsUnique();
        builder.HasIndex(f => new { f.TenantId, f.SubmittedAt });
    }
}
