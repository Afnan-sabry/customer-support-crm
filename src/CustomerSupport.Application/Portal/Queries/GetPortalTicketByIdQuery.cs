using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Portal.Queries;

public record GetPortalTicketByIdQuery(Guid TicketId, Guid CustomerId) : IRequest<PortalTicketDetailDto?>;

public class GetPortalTicketByIdQueryHandler : IRequestHandler<GetPortalTicketByIdQuery, PortalTicketDetailDto?>
{
    private readonly AppDbContext _context;

    public GetPortalTicketByIdQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PortalTicketDetailDto?> Handle(GetPortalTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.Status)
            .Include(t => t.Comments.Where(c => !c.IsInternal).OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.CustomerId == request.CustomerId, cancellationToken);

        if (ticket is null) return null;

        var comments = ticket.Comments.Select(c => new PortalCommentDto(
            c.Id, c.Content,
            c.UserId != null ? (c.User?.FullName ?? "Agent") : "You",
            c.CreatedAt,
            c.UserId != null)).ToList();

        return new PortalTicketDetailDto(
            ticket.Id, ticket.TicketNumber, ticket.Subject, ticket.Description,
            ticket.Category?.Name ?? "", ticket.Priority?.Name ?? "", ticket.Status?.Name ?? "",
            ticket.CreatedAt, ticket.UpdatedAt, comments);
    }
}
