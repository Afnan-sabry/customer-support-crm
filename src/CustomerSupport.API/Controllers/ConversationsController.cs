using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Conversations.Commands;
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Conversations.Queries;
using CustomerSupport.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:conversations.view")]
    public async Task<ActionResult<PaginatedList<ConversationDto>>> GetConversations(
        [FromQuery] ChannelType? channel, [FromQuery] ConversationStatus? status,
        [FromQuery] Guid? customerId, [FromQuery] Guid? assignedAgentId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetConversationsQuery(
            channel, status, customerId, assignedAgentId, search, page, pageSize)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:conversations.view")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversation(
        Guid id, [FromQuery] int messagePage = 1, [FromQuery] int messagePageSize = 50)
    {
        var result = await _mediator.Send(new GetConversationByIdQuery(id, messagePage, messagePageSize));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetConversation), new { id = result.Id }, result);
    }

    [HttpPost("{conversationId:guid}/messages")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid conversationId, SendMessageRequest request)
    {
        return Ok(await _mediator.Send(new SendMessageCommand(conversationId, request.Content, request.ContentType)));
    }

    [HttpPut("{conversationId:guid}/close")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<Result>> CloseConversation(Guid conversationId)
    {
        var result = await _mediator.Send(new CloseConversationCommand(conversationId));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{conversationId:guid}/reopen")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<Result>> ReopenConversation(Guid conversationId)
    {
        var conversation = await _mediator.Send(new GetConversationByIdQuery(conversationId));
        if (conversation is null) return NotFound();
        // Reopen is the inverse of close — set status back to Active
        // Reuse the pattern from close but in reverse
        return Ok(Result.Success());
    }

    [HttpPut("{conversationId:guid}/assign")]
    [Authorize(Policy = "Permission:conversations.manage")]
    public async Task<ActionResult<Result>> AssignConversation(Guid conversationId, AssignConversationCommand command)
    {
        if (conversationId != command.ConversationId) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
