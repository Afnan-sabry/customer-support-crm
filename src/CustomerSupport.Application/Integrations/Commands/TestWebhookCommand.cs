using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Integrations.Commands;

public record TestWebhookCommand(Guid SubscriptionId) : IRequest<Result>;

public class TestWebhookCommandHandler : IRequestHandler<TestWebhookCommand, Result>
{
    private readonly AppDbContext _context;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly ICurrentUserService _currentUserService;

    public TestWebhookCommandHandler(AppDbContext context, IWebhookDispatcher dispatcher, ICurrentUserService currentUserService)
    {
        _context = context;
        _dispatcher = dispatcher;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(TestWebhookCommand request, CancellationToken cancellationToken)
    {
        var sub = await _context.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);
        if (sub is null) return Result.Failure("Subscription not found.");

        await _dispatcher.DispatchAsync(_currentUserService.TenantId, "test.ping", new
        {
            message = "Test webhook delivery",
            timestamp = DateTime.UtcNow
        });

        return Result.Success();
    }
}
