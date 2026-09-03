using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Domain.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid tenantId, string templateKey, NotificationRecipientInfo recipient, Dictionary<string, string> placeholders, string? data = null);
}

public record NotificationRecipientInfo(Guid Id, RecipientType Type, string? Email = null, string? Phone = null);
