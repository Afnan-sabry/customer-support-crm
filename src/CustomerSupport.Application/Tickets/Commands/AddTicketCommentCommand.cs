using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Tickets.Commands;

public record AddTicketCommentCommand(Guid TicketId, string Content, bool IsInternal) : IRequest<TicketCommentDto>;

public class AddTicketCommentCommandHandler : IRequestHandler<AddTicketCommentCommand, TicketCommentDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddTicketCommentCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        return new TicketCommentDto(comment.Id, comment.UserId, userName, comment.Content, comment.IsInternal, comment.CreatedAt);
    }
}
