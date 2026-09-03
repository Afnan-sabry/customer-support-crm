using System.Text.Json;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Queries;

public record GetWebhookSubscriptionsQuery : IRequest<List<WebhookSubscriptionDto>>;

public class GetWebhookSubscriptionsQueryHandler : IRequestHandler<GetWebhookSubscriptionsQuery, List<WebhookSubscriptionDto>>
{
    private readonly AppDbContext _context;

    public GetWebhookSubscriptionsQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<WebhookSubscriptionDto>> Handle(GetWebhookSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.WebhookSubscriptions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new WebhookSubscriptionDto(
                s.Id, s.Name, s.Url,
                JsonSerializer.Deserialize<string[]>(s.Events) ?? Array.Empty<string>(),
                s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
