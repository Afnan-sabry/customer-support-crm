using CustomerSupport.API.Hubs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CustomerSupport.API.Services;

public class InAppNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public InAppNotificationDispatcher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public string Channel => "InApp";

    public async Task DispatchAsync(Notification notification, NotificationRecipientInfo recipient)
    {
        await _hubContext.Clients.Group($"notifications-{recipient.Id}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.TitleAr,
                notification.Body,
                notification.BodyAr,
                notification.Data,
                notification.CreatedAt
            });
    }
}
