using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Escalation.Commands;

public record DeleteEscalationRuleCommand(Guid Id) : IRequest<Result>;

public class DeleteEscalationRuleCommandHandler : IRequestHandler<DeleteEscalationRuleCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteEscalationRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteEscalationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.EscalationRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Escalation rule not found.");

        rule.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
