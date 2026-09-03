using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Commands;

public record UpdateTicketStatusCommand(Guid TicketId, Guid StatusId) : IRequest<Result>;

public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;

    public UpdateTicketStatusCommandHandler(
        ITicketRepository ticketRepository, AppDbContext context,
        ICurrentUserService currentUserService, IPublisher publisher)
    {
        _ticketRepository = ticketRepository;
        _context = context;
        _currentUserService = currentUserService;
        _publisher = publisher;
    }

    public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null) return Result.Failure("Ticket not found.");

        var oldStatusId = ticket.StatusId;
        var oldStatus = await _context.TicketStatuses.FindAsync([ticket.StatusId], cancellationToken);
        var newStatus = await _context.TicketStatuses.FindAsync([request.StatusId], cancellationToken);

        _context.TicketHistories.Add(new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId,
            Field = "Status",
            OldValue = oldStatus?.Name,
            NewValue = newStatus?.Name,
            CreatedAt = DateTime.UtcNow
        });

        ticket.StatusId = request.StatusId;
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);

        try
        {
            await _publisher.Publish(new TicketStatusChangedNotification(
                ticket.Id, ticket.TenantId, oldStatusId, request.StatusId), cancellationToken);
        }
        catch { }

        return Result.Success();
    }
}
