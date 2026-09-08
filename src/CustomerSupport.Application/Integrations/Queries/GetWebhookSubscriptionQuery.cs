using System.Text.Json;
using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Queries;

public record GetWebhookSubscriptionQuery(Guid Id) : IRequest<WebhookSubscriptionDto?>;

public class GetWebhookSubscriptionQueryHandler : IRequestHandler<GetWebhookSubscriptionQuery, WebhookSubscriptionDto?>
{
    private readonly AppDbContext _context;

    public GetWebhookSubscriptionQueryHandler(AppDbContext context) => _context = context;

    public async Task<WebhookSubscriptionDto?> Handle(GetWebhookSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var s = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (s is null) return null;
        return new WebhookSubscriptionDto(s.Id, s.Name, s.Url,
            JsonSerializer.Deserialize<string[]>(s.Events) ?? Array.Empty<string>(),
            s.IsActive, s.CreatedAt);
    }
}
