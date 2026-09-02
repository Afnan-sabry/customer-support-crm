using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class SlaBreachLogConfiguration : IEntityTypeConfiguration<SlaBreachLog>
{
    public void Configure(EntityTypeBuilder<SlaBreachLog> builder)
    {
        builder.ToTable("SlaBreachLogs");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BreachType).IsRequired().HasMaxLength(50);

        builder.HasOne(b => b.Ticket).WithMany().HasForeignKey(b => b.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(b => b.SlaPolicy).WithMany().HasForeignKey(b => b.SlaPolicyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.TenantId, b.TicketId });
    }
}
