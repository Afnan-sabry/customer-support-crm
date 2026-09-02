using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByIdWithMessagesAsync(Guid id, int messagePage = 1, int messagePageSize = 50, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken = default);
    IQueryable<Conversation> GetQueryable();
}
