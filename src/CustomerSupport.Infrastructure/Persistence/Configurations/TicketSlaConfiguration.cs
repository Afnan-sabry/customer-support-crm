using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class TicketSlaConfiguration : IEntityTypeConfiguration<TicketSla>
{
    public void Configure(EntityTypeBuilder<TicketSla> builder)
    {
        builder.ToTable("TicketSlas");
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Ticket).WithMany().HasForeignKey(t => t.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.SlaPolicy).WithMany().HasForeignKey(t => t.SlaPolicyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.TicketId).IsUnique();
        builder.HasIndex(t => new { t.TenantId, t.FirstResponseBreached });
        builder.HasIndex(t => new { t.TenantId, t.ResolutionBreached });
    }
}
