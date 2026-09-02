using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface ISlaRepository : IRepository<SlaPolicy>
{
    IQueryable<SlaPolicy> GetQueryable();
    Task<SlaPolicy?> FindBestPolicyAsync(Guid tenantId, Guid? priorityId, Guid? categoryId, CancellationToken cancellationToken = default);
}
