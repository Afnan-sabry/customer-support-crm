using System.Text.Json;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;

namespace CustomerSupport.Application.Integrations.Commands;

public record CreateWebhookSubscriptionCommand(
    string Name, string Url, string Secret, string[] Events,
    bool IsActive, Dictionary<string, string>? Headers = null) : IRequest<WebhookSubscriptionDto>;

public class CreateWebhookSubscriptionCommandHandler : IRequestHandler<CreateWebhookSubscriptionCommand, WebhookSubscriptionDto>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateWebhookSubscriptionCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<WebhookSubscriptionDto> Handle(CreateWebhookSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUserService.TenantId,
            Name = request.Name,
            Url = request.Url,
            Secret = request.Secret,
            Events = JsonSerializer.Serialize(request.Events),
            IsActive = request.IsActive,
            Headers = request.Headers is not null ? JsonSerializer.Serialize(request.Headers) : null
        };

        _context.WebhookSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return new WebhookSubscriptionDto(
            subscription.Id, subscription.Name, subscription.Url,
            request.Events, subscription.IsActive, subscription.CreatedAt);
    }
}
