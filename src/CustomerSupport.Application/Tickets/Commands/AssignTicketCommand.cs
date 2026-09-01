using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Tickets.Commands;

public record AssignTicketCommand(Guid TicketId, Guid? AssignedToId) : IRequest<Result>;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public AssignTicketCommandHandler(ITicketRepository ticketRepository, AppDbContext context, UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null) return Result.Failure("Ticket not found.");

        var oldAssignee = ticket.AssignedToId.HasValue ? await _userManager.FindByIdAsync(ticket.AssignedToId.Value.ToString()) : null;
        var newAssignee = request.AssignedToId.HasValue ? await _userManager.FindByIdAsync(request.AssignedToId.Value.ToString()) : null;

        _context.TicketHistories.Add(new TicketHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            UserId = _currentUserService.UserId == Guid.Empty ? null : _currentUserService.UserId,
            Field = "AssignedTo",
            OldValue = oldAssignee?.FullName,
            NewValue = newAssignee?.FullName,
            CreatedAt = DateTime.UtcNow
        });

        ticket.AssignedToId = request.AssignedToId;
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        return Result.Success();
    }
}
