using CustomerSupport.Application.Common.Notifications;
using CustomerSupport.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CustomerSupport.Application.Ai.Handlers;

public class AiCategorizationHandler : MediatR.INotificationHandler<TicketCreatedNotification>
{
    private readonly IAiTicketService _aiTicketService;
    private readonly ILogger<AiCategorizationHandler> _logger;

    public AiCategorizationHandler(IAiTicketService aiTicketService, ILogger<AiCategorizationHandler> logger)
    {
        _aiTicketService = aiTicketService;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _aiTicketService.CategorizeAsync(notification.TicketId, cancellationToken);
            _logger.LogInformation(
                "AI categorized ticket {TicketId}: Category={Category}, Priority={Priority}, Confidence={Confidence}, AutoApplied={AutoApplied}",
                notification.TicketId, result.SuggestedCategoryName, result.SuggestedPriorityName,
                result.Confidence, result.AutoApplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI categorization failed for ticket {TicketId} — skipping", notification.TicketId);
        }
    }
}
