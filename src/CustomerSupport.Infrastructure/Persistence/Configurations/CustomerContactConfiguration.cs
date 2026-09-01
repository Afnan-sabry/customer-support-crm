using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts");
        builder.HasKey(cc => cc.Id);
        builder.Property(cc => cc.Name).IsRequired().HasMaxLength(200);
        builder.Property(cc => cc.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(cc => cc.Email).HasMaxLength(200);
        builder.Property(cc => cc.Phone).HasMaxLength(20);
        builder.Property(cc => cc.Title).HasMaxLength(100);
    }
}
