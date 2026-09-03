using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Ai.Commands;

public record SummarizeTicketCommand(Guid TicketId) : IRequest<AiSummaryResult>;

public class SummarizeTicketCommandHandler : IRequestHandler<SummarizeTicketCommand, AiSummaryResult>
{
    private readonly IAiTicketService _aiTicketService;

    public SummarizeTicketCommandHandler(IAiTicketService aiTicketService) => _aiTicketService = aiTicketService;

    public async Task<AiSummaryResult> Handle(SummarizeTicketCommand request, CancellationToken cancellationToken)
        => await _aiTicketService.SummarizeAsync(request.TicketId, cancellationToken);
}
