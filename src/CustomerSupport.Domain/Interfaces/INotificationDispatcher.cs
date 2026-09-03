using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface INotificationDispatcher
{
    string Channel { get; }
    Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient);
}
