using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Repositories;

public class SlaRepository : ISlaRepository
{
    private readonly AppDbContext _context;

    public SlaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SlaPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.SlaPolicies.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<SlaPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.SlaPolicies.ToListAsync(cancellationToken);

    public async Task<SlaPolicy> AddAsync(SlaPolicy entity, CancellationToken cancellationToken = default)
    {
        await _context.SlaPolicies.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(SlaPolicy entity, CancellationToken cancellationToken = default)
    {
        _context.SlaPolicies.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<SlaPolicy> GetQueryable() => _context.SlaPolicies.AsQueryable();

    public async Task<SlaPolicy?> FindBestPolicyAsync(Guid tenantId, Guid? priorityId, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var policies = await _context.SlaPolicies
            .Where(p => p.IsActive)
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return policies.FirstOrDefault(p => p.PriorityId == priorityId && p.CategoryId == categoryId)
            ?? policies.FirstOrDefault(p => p.PriorityId == priorityId && p.CategoryId == null)
            ?? policies.FirstOrDefault(p => p.PriorityId == null && p.CategoryId == categoryId)
            ?? policies.FirstOrDefault(p => p.PriorityId == null && p.CategoryId == null);
    }
}
