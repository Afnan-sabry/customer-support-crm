using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Conversations.Commands;

public record AssignConversationCommand(Guid ConversationId, Guid AgentId) : IRequest<Result>;

public class AssignConversationCommandHandler : IRequestHandler<AssignConversationCommand, Result>
{
    private readonly AppDbContext _context;

    public AssignConversationCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation is null) return Result.Failure(["Conversation not found"]);

        var agent = await _context.Users.FindAsync([request.AgentId], cancellationToken);
        if (agent is null) return Result.Failure(["Agent not found"]);

        if (agent.TenantId != conversation.TenantId)
            return Result.Failure(["Agent not found"]);

        conversation.AssignedAgentId = request.AgentId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
