using CustomerSupport.Application.Integrations.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Queries;

public record GetWebhookDeliveryLogsQuery(Guid SubscriptionId, int Page = 1, int PageSize = 20) : IRequest<List<WebhookDeliveryLogDto>>;

public class GetWebhookDeliveryLogsQueryHandler : IRequestHandler<GetWebhookDeliveryLogsQuery, List<WebhookDeliveryLogDto>>
{
    private readonly AppDbContext _context;

    public GetWebhookDeliveryLogsQueryHandler(AppDbContext context) => _context = context;

    public async Task<List<WebhookDeliveryLogDto>> Handle(GetWebhookDeliveryLogsQuery request, CancellationToken cancellationToken)
    {
        return await _context.WebhookDeliveryLogs
            .Where(d => d.SubscriptionId == request.SubscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new WebhookDeliveryLogDto(
                d.Id, d.Event, d.StatusCode, d.Success,
                d.Attempt, d.ErrorMessage, d.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
