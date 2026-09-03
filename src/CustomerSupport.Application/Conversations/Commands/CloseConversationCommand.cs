using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Conversations.Commands;

public record CloseConversationCommand(Guid ConversationId) : IRequest<Result>;

public class CloseConversationCommandHandler : IRequestHandler<CloseConversationCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public CloseConversationCommandHandler(AppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(CloseConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(["Conversation not found"]);
        if (conversation.Status == ConversationStatus.Closed)
            return Result.Failure(["Conversation is already closed"]);

        conversation.Status = ConversationStatus.Closed;
        conversation.ClosedAt = _dateTimeService.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
