using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IChatSessionService
{
    Task<Conversation> StartSessionAsync(Guid customerId, Guid tenantId, string? subject = null);
    Task EndSessionAsync(Guid conversationId);
    Task<int> GetQueuePositionAsync(Guid conversationId);
    Task<List<Conversation>> GetActiveSessionsAsync(Guid? agentId = null);
}
