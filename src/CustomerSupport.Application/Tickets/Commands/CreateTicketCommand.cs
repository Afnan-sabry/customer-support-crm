using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Commands;

public record CreateTicketCommand(
    Guid CustomerId, Guid CategoryId, Guid PriorityId,
    string Subject, string Description) : IRequest<TicketDto>;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateTicketCommandHandler(ITicketRepository ticketRepository, AppDbContext context, ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var newStatus = await _context.TicketStatuses
            .FirstOrDefaultAsync(s => s.Name == "New", cancellationToken)
            ?? throw new InvalidOperationException("Default ticket status 'New' not found.");

        var ticketNumber = await _ticketRepository.GenerateTicketNumberAsync(cancellationToken);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            TicketNumber = ticketNumber,
            CustomerId = request.CustomerId,
            CategoryId = request.CategoryId,
            PriorityId = request.PriorityId,
            StatusId = newStatus.Id,
            Subject = request.Subject,
            Description = request.Description
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);

        var customer = await _context.Customers.FindAsync([request.CustomerId], cancellationToken);
        var category = await _context.TicketCategories.FindAsync([request.CategoryId], cancellationToken);
        var priority = await _context.TicketPriorities.FindAsync([request.PriorityId], cancellationToken);

        return new TicketDto(
            ticket.Id, ticket.TicketNumber, ticket.Subject,
            customer?.Name ?? "", category?.Name ?? "", priority?.Name ?? "",
            newStatus.Name, null, ticket.CreatedAt);
    }
}
