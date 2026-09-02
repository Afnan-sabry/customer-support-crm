namespace CustomerSupport.Application.Knowledge.DTOs;

public record KnowledgeCategoryDto(Guid Id, string Name, string NameAr, Guid? ParentCategoryId, int Order, bool IsActive);
