namespace CustomerSupport.Domain.Entities;

public class KnowledgeArticle : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? Tags { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public bool IsActive { get; set; } = true;

    public KnowledgeCategory Category { get; set; } = null!;
}
