using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;

namespace CustomerSupport.Infrastructure.Services.Dispatchers;

public class EmailNotificationDispatcher : INotificationDispatcher
{
    private readonly IEmailSender _emailSender;

    public EmailNotificationDispatcher(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public string Channel => "Email";

    public async Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient)
    {
        if (string.IsNullOrEmpty(recipient.Email)) return;
        await _emailSender.SendAsync(recipient.Email, notification.Title, notification.Body, null);
    }
}
