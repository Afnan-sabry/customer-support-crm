using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired();

        builder.HasIndex(c => c.TicketId);

        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
    }
}
