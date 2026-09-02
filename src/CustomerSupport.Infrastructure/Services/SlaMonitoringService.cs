using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Infrastructure.Services;

public class SlaMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitoringService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SlaMonitoringService(IServiceScopeFactory scopeFactory, ILogger<SlaMonitoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckForBreachesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SLA breaches");
            }
        }
    }

    private async Task CheckForBreachesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dateTimeService = scope.ServiceProvider.GetRequiredService<IDateTimeService>();
        var now = dateTimeService.UtcNow;

        var finalStatusIds = await context.TicketStatuses
            .Where(s => s.IsFinal)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var newBreachLogs = new List<SlaBreachLog>();

        var firstResponseBreaches = await context.TicketSlas
            .IgnoreQueryFilters()
            .Where(ts => !ts.FirstResponseBreached && ts.FirstRespondedAt == null && ts.FirstResponseDue < now)
            .Where(ts => !finalStatusIds.Contains(ts.Ticket.StatusId))
            .ToListAsync(cancellationToken);

        foreach (var sla in firstResponseBreaches)
        {
            sla.FirstResponseBreached = true;
            var breachLog = new SlaBreachLog
            {
                Id = Guid.NewGuid(),
                TenantId = sla.TenantId,
                TicketId = sla.TicketId,
                SlaPolicyId = sla.SlaPolicyId,
                BreachType = "FirstResponse",
                DueAt = sla.FirstResponseDue,
                BreachedAt = now
            };
            context.SlaBreachLogs.Add(breachLog);
            newBreachLogs.Add(breachLog);
        }

        var resolutionBreaches = await context.TicketSlas
            .IgnoreQueryFilters()
            .Where(ts => !ts.ResolutionBreached && ts.ResolvedAt == null && ts.ResolutionDue < now)
            .Where(ts => !finalStatusIds.Contains(ts.Ticket.StatusId))
            .ToListAsync(cancellationToken);

        foreach (var sla in resolutionBreaches)
        {
            sla.ResolutionBreached = true;
            var breachLog = new SlaBreachLog
            {
                Id = Guid.NewGuid(),
                TenantId = sla.TenantId,
                TicketId = sla.TicketId,
                SlaPolicyId = sla.SlaPolicyId,
                BreachType = "Resolution",
                DueAt = sla.ResolutionDue,
                BreachedAt = now
            };
            context.SlaBreachLogs.Add(breachLog);
            newBreachLogs.Add(breachLog);
        }

        if (firstResponseBreaches.Count > 0 || resolutionBreaches.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SLA breaches detected: {FirstResponse} first-response, {Resolution} resolution",
                firstResponseBreaches.Count, resolutionBreaches.Count);

            var escalationService = scope.ServiceProvider.GetRequiredService<EscalationService>();
            foreach (var breach in newBreachLogs)
            {
                try
                {
                    await escalationService.ProcessBreachAsync(breach, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing escalation for breach {BreachId}", breach.Id);
                }
            }
        }
    }
}
