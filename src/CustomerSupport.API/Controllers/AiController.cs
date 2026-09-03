using CustomerSupport.Application.Ai.Commands;
using CustomerSupport.Application.Ai.DTOs;
using CustomerSupport.Application.Ai.Queries;
using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiController(IMediator mediator) => _mediator = mediator;

    [HttpPost("tickets/{ticketId:guid}/categorize")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<AiCategorizationResult>> CategorizeTicket(Guid ticketId)
        => Ok(await _mediator.Send(new CategorizeTicketCommand(ticketId)));

    [HttpPost("tickets/{ticketId:guid}/summarize")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<AiSummaryResult>> SummarizeTicket(Guid ticketId)
        => Ok(await _mediator.Send(new SummarizeTicketCommand(ticketId)));

    [HttpPost("tickets/{ticketId:guid}/suggest-replies")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<AiSuggestedRepliesResult>> SuggestReplies(Guid ticketId)
        => Ok(await _mediator.Send(new SuggestReplyCommand(ticketId)));

    [HttpGet("tickets/{ticketId:guid}/suggestions")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<List<AiSuggestionDto>>> GetSuggestions(Guid ticketId)
        => Ok(await _mediator.Send(new GetTicketSuggestionsQuery(ticketId)));

    [HttpPut("suggestions/{suggestionId:guid}/accept")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<Result>> AcceptSuggestion(Guid suggestionId)
    {
        var result = await _mediator.Send(new AcceptAiSuggestionCommand(suggestionId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("suggestions/{suggestionId:guid}/reject")]
    [Authorize(Policy = "Permission:ai.use")]
    public async Task<ActionResult<Result>> RejectSuggestion(Guid suggestionId)
    {
        var result = await _mediator.Send(new RejectAiSuggestionCommand(suggestionId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
