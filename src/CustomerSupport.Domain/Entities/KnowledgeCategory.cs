namespace CustomerSupport.Domain.Entities;

public class KnowledgeCategory : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public KnowledgeCategory? ParentCategory { get; set; }
    public ICollection<KnowledgeCategory> SubCategories { get; set; } = [];
    public ICollection<KnowledgeArticle> Articles { get; set; } = [];
}
