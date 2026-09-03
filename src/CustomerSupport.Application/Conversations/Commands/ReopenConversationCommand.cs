using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Enums;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Conversations.Commands;

public record ReopenConversationCommand(Guid ConversationId) : IRequest<Result>;

public class ReopenConversationCommandHandler : IRequestHandler<ReopenConversationCommand, Result>
{
    private readonly AppDbContext _context;

    public ReopenConversationCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ReopenConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(["Conversation not found"]);
        if (conversation.Status != ConversationStatus.Closed)
            return Result.Failure(["Conversation is not closed"]);

        conversation.Status = ConversationStatus.Active;
        conversation.ClosedAt = null;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
