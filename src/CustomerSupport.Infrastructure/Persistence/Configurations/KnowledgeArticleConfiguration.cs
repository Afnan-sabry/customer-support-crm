using CustomerSupport.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerSupport.Infrastructure.Persistence.Configurations;

public class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(500);
        builder.Property(a => a.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Content).IsRequired();
        builder.Property(a => a.ContentAr).IsRequired();
        builder.Property(a => a.Tags).HasMaxLength(2000);

        builder.HasOne(a => a.Category).WithMany(c => c.Articles)
            .HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.CategoryId });
        builder.HasIndex(a => new { a.TenantId, a.IsPublished });
    }
}
