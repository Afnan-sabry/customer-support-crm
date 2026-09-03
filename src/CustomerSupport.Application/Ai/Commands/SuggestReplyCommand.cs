using CustomerSupport.Domain.Interfaces;
using MediatR;

namespace CustomerSupport.Application.Ai.Commands;

public record SuggestReplyCommand(Guid TicketId) : IRequest<AiSuggestedRepliesResult>;

public class SuggestReplyCommandHandler : IRequestHandler<SuggestReplyCommand, AiSuggestedRepliesResult>
{
    private readonly IAiTicketService _aiTicketService;

    public SuggestReplyCommandHandler(IAiTicketService aiTicketService) => _aiTicketService = aiTicketService;

    public async Task<AiSuggestedRepliesResult> Handle(SuggestReplyCommand request, CancellationToken cancellationToken)
        => await _aiTicketService.SuggestRepliesAsync(request.TicketId, cancellationToken);
}
