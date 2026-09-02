namespace CustomerSupport.Application.Knowledge.DTOs;

public record KnowledgeArticleDetailDto(
    Guid Id, string Title, string TitleAr,
    string Content, string ContentAr,
    Guid CategoryId, string CategoryName,
    string? Tags, bool IsPublished, int ViewCount,
    DateTime CreatedAt, DateTime UpdatedAt);
