using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Ai.Commands;

public record CategorizeTicketCommand(Guid TicketId) : IRequest<AiCategorizationResult>;

public class CategorizeTicketCommandHandler : IRequestHandler<CategorizeTicketCommand, AiCategorizationResult>
{
    private readonly IAiTicketService _aiTicketService;

    public CategorizeTicketCommandHandler(IAiTicketService aiTicketService) => _aiTicketService = aiTicketService;

    public async Task<AiCategorizationResult> Handle(CategorizeTicketCommand request, CancellationToken cancellationToken)
        => await _aiTicketService.CategorizeAsync(request.TicketId, cancellationToken);
}
