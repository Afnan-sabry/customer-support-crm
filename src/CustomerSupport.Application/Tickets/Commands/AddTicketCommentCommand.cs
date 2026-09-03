using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Tickets.Commands;

public record AddTicketCommentCommand(Guid TicketId, string Content, bool IsInternal) : IRequest<TicketCommentDto>;

public class AddTicketCommentCommandHandler : IRequestHandler<AddTicketCommentCommand, TicketCommentDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublisher _publisher;
    private readonly ILogger<AddTicketCommentCommandHandler> _logger;

    public AddTicketCommentCommandHandler(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IPublisher publisher,
        ILogger<AddTicketCommentCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<TicketCommentDto> Handle(AddTicketCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            UserId = _currentUserService.UserId,
            Content = request.Content,
            IsInternal = request.IsInternal
        };

        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        var userName = await _context.Users
            .Where(u => u.Id == _currentUserService.UserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "";

        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket != null)
        {
            try
            {
                await _publisher.Publish(new TicketCommentAddedNotification(
                    request.TicketId, _currentUserService.UserId, ticket.TenantId), cancellationToken);
            }
            catch (Exception ex)
            {
                // SLA first-response tracking (and any other TicketCommentAddedNotification handler)
                // must never fail comment creation. The comment is already committed at this point,
                // so a downstream handler failure is logged and swallowed rather than propagated.
                _logger.LogError(ex, "Failed to publish TicketCommentAddedNotification for ticket {TicketId}", request.TicketId);
            }
        }

        return new TicketCommentDto(comment.Id, comment.UserId ?? Guid.Empty, userName, comment.Content, comment.IsInternal, comment.CreatedAt);
    }
}
