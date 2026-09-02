using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Sla.Handlers;

public class MarkFirstResponseHandler : INotificationHandler<TicketCommentAddedNotification>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public MarkFirstResponseHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(TicketCommentAddedNotification notification, CancellationToken cancellationToken)
    {
        var isAgent = notification.CommentUserId != Guid.Empty;
        if (!isAgent) return;

        var ticketSla = await _context.TicketSlas
            .FirstOrDefaultAsync(ts => ts.TicketId == notification.TicketId && ts.FirstRespondedAt == null, cancellationToken);

        if (ticketSla == null) return;

        ticketSla.FirstRespondedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
