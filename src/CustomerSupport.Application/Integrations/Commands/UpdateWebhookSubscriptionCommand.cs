using System.Text.Json;
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Commands;

public record UpdateWebhookSubscriptionCommand(
    Guid Id, string Name, string Url, string Secret,
    string[] Events, bool IsActive, Dictionary<string, string>? Headers = null) : IRequest<Result>;

public class UpdateWebhookSubscriptionCommandHandler : IRequestHandler<UpdateWebhookSubscriptionCommand, Result>
{
    private readonly AppDbContext _context;

    public UpdateWebhookSubscriptionCommandHandler(AppDbContext context) => _context = context;

    public async Task<Result> Handle(UpdateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (sub is null) return Result.Failure("Subscription not found.");

        sub.Name = request.Name;
        sub.Url = request.Url;
        sub.Secret = request.Secret;
        sub.Events = JsonSerializer.Serialize(request.Events);
        sub.IsActive = request.IsActive;
        sub.Headers = request.Headers is not null ? JsonSerializer.Serialize(request.Headers) : null;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
