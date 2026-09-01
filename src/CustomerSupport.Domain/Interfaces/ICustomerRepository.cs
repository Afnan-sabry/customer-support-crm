using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByIdWithContactsAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<Customer> GetQueryable();
}
