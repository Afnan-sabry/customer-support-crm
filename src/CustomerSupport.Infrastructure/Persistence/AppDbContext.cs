using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService) : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter for tenant isolation on all ITenantEntity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, [modelBuilder, _currentUserService]);
            }
        }
    }

    private static void ApplyTenantFilter<T>(ModelBuilder modelBuilder, ICurrentUserService currentUserService)
        where T : class, ITenantEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == currentUserService.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = _dateTimeService.UtcNow;
                    entry.Entity.UpdatedAt = _dateTimeService.UtcNow;
                    entry.Entity.CreatedBy = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTimeService.UtcNow;
                    entry.Entity.UpdatedBy = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
