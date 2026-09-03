namespace CustomerSupport.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id, string Title, string TitleAr,
    string Body, string BodyAr, string? Data,
    bool IsRead, DateTime CreatedAt, DateTime? ReadAt);
