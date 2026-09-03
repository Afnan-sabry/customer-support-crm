namespace CustomerSupport.Application.Knowledge.DTOs;

public record KnowledgeArticleDto(
    Guid Id, string Title, string TitleAr,
    Guid CategoryId, string CategoryName,
    string? Tags, bool IsPublished, int ViewCount, DateTime CreatedAt);
