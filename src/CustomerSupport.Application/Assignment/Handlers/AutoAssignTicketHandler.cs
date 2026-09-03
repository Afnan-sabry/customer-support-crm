using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using CustomerSupport.Infrastructure.Services;
using MediatR;

namespace CustomerSupport.Application.Assignment.Handlers;

public class AutoAssignTicketHandler : INotificationHandler<TicketCreatedNotification>
{
    private readonly AppDbContext _context;
    private readonly AssignmentService _assignmentService;

    public AutoAssignTicketHandler(AppDbContext context, AssignmentService assignmentService)
    {
        _context = context;
        _assignmentService = assignmentService;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FindAsync([notification.TicketId], cancellationToken);
        if (ticket == null || ticket.AssignedToId.HasValue) return;

        var assigneeId = await _assignmentService.FindAssigneeAsync(
            notification.TenantId, notification.CategoryId, notification.PriorityId, cancellationToken);

        if (!assigneeId.HasValue) return;

        ticket.AssignedToId = assigneeId.Value;
        _context.Set<TicketHistory>().Add(new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Field = "AssignedToId",
            OldValue = null,
            NewValue = assigneeId.Value.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
