using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Assignment.Commands;

public record DeleteAssignmentRuleCommand(Guid Id) : IRequest<Result>;

public class DeleteAssignmentRuleCommandHandler : IRequestHandler<DeleteAssignmentRuleCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteAssignmentRuleCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteAssignmentRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _context.AssignmentRules.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException("Assignment rule not found.");

        rule.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
