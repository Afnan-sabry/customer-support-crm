using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Dispatchers;

public class SmsNotificationDispatcher : INotificationDispatcher
{
    private readonly ISmsClient _smsClient;

    public SmsNotificationDispatcher(ISmsClient smsClient)
    {
        _smsClient = smsClient;
    }

    public string Channel => "SMS";

    public async Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient)
    {
        if (string.IsNullOrEmpty(recipient.Phone)) return;
        await _smsClient.SendAsync(recipient.Phone, notification.Body);
    }
}
