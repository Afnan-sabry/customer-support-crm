using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class KnowledgeCategoryConfiguration : IEntityTypeConfiguration<KnowledgeCategory>
{
    public void Configure(EntityTypeBuilder<KnowledgeCategory> builder)
    {
        builder.ToTable("KnowledgeCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);

        builder.HasOne(c => c.ParentCategory).WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.ParentCategoryId });
    }
}
