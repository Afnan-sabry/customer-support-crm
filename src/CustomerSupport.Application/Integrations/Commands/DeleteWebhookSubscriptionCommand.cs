using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Commands;

public record DeleteWebhookSubscriptionCommand(Guid Id) : IRequest<Result>;

public class DeleteWebhookSubscriptionCommandHandler : IRequestHandler<DeleteWebhookSubscriptionCommand, Result>
{
    private readonly AppDbContext _context;

    public DeleteWebhookSubscriptionCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (sub is null) return Result.Failure("Subscription not found.");

        _context.WebhookSubscriptions.Remove(sub);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
