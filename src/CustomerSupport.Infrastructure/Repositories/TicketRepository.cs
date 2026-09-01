using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Tickets.FindAsync([id], cancellationToken);

    public async Task<Ticket?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Status)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
            .Include(t => t.Attachments)
            .Include(t => t.History.OrderByDescending(h => h.CreatedAt))
                .ThenInclude(h => h.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Tickets.ToListAsync(cancellationToken);

    public async Task<Ticket> AddAsync(Ticket entity, CancellationToken cancellationToken = default)
    {
        await _context.Tickets.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Ticket entity, CancellationToken cancellationToken = default)
    {
        _context.Tickets.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<Ticket> GetQueryable() => _context.Tickets.AsQueryable();

    public async Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"TKT-{today}-";
        var lastNumber = await _context.Tickets
            .Where(t => t.TicketNumber.StartsWith(prefix))
            .Select(t => t.TicketNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (lastNumber != null)
        {
            var suffix = lastNumber.Substring(prefix.Length);
            if (int.TryParse(suffix, out var parsed))
                next = parsed + 1;
        }
        return $"{prefix}{next:D4}";
    }
}
