namespace CustomerSupport.Domain.Interfaces;

public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid tenantId, string eventName, object payload);
}
