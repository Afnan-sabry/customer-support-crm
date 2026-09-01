using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Domain.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<Ticket> GetQueryable();
    Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default);
}
