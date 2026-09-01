using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Commands;

public record ChangeTicketPriorityCommand(Guid TicketId, Guid PriorityId) : IRequest<Result>;

public class ChangeTicketPriorityCommandHandler : IRequestHandler<ChangeTicketPriorityCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ChangeTicketPriorityCommandHandler(ITicketRepository ticketRepository, AppDbContext context, ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ChangeTicketPriorityCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null) return Result.Failure("Ticket not found.");

        var oldPriority = await _context.TicketPriorities.FindAsync([ticket.PriorityId], cancellationToken);
        var newPriority = await _context.TicketPriorities.FindAsync([request.PriorityId], cancellationToken);

        _context.TicketHistories.Add(new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId,
            Field = "Priority",
            OldValue = oldPriority?.Name,
            NewValue = newPriority?.Name,
            CreatedAt = DateTime.UtcNow
        });

        ticket.PriorityId = request.PriorityId;
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        return Result.Success();
    }
}
