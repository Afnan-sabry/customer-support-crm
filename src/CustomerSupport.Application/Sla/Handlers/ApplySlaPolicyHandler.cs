using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Sla.Handlers;

public class ApplySlaPolicyHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly ISlaRepository _slaRepository;
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public ApplySlaPolicyHandler(ISlaRepository slaRepository, AppDbContext context, IDateTimeService dateTimeService)
    {
        _slaRepository = slaRepository;
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var policy = await _slaRepository.FindBestPolicyAsync(
            notification.TenantId, notification.PriorityId, notification.CategoryId, cancellationToken);

        if (policy == null) return;

        var now = _dateTimeService.UtcNow;
        var ticketSla = new TicketSla
        {
            Id = Guid.NewGuid(),
            TenantId = notification.TenantId,
            TicketId = notification.TicketId,
            SlaPolicyId = policy.Id,
            FirstResponseDue = now.AddMinutes(policy.FirstResponseMinutes),
            ResolutionDue = now.AddMinutes(policy.ResolutionMinutes)
        };

        await _context.TicketSlas.AddAsync(ticketSla, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
