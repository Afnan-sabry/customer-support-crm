using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Conversations.Commands;
using CustomerSupport.Application.Conversations.DTOs;
using CustomerSupport.Application.Knowledge.DTOs;
using CustomerSupport.Application.Knowledge.Queries;
using CustomerSupport.Application.Portal.Commands;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Application.Portal.Queries;
using CustomerSupport.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/portal")]
[Authorize(AuthenticationSchemes = "Portal")]
public class PortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetCustomerId() =>
        Guid.Parse(User.FindFirst("CustomerId")!.Value);

    private Guid GetPortalUserId() =>
        Guid.Parse(User.FindFirst("PortalUserId")!.Value);

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirst("TenantId")!.Value);

    [HttpGet("tickets")]
    public async Task<ActionResult<PaginatedList<PortalTicketDto>>> GetTickets(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetPortalTicketsQuery(GetCustomerId(), page, pageSize)));
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<PortalTicketDto>> SubmitTicket(PortalTicketRequest request)
    {
        var result = await _mediator.Send(new PortalSubmitTicketCommand(
            GetCustomerId(), GetTenantId(),
            request.CategoryId, request.PriorityId,
            request.Subject, request.Description));
        return CreatedAtAction(nameof(GetTicket), new { id = result.Id }, result);
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<ActionResult<PortalTicketDetailDto>> GetTicket(Guid id)
    {
        var result = await _mediator.Send(new GetPortalTicketByIdQuery(id, GetCustomerId()));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("tickets/{ticketId:guid}/comments")]
    public async Task<ActionResult<PortalCommentDto>> AddComment(Guid ticketId, [FromBody] PortalAddCommentRequest request)
    {
        var result = await _mediator.Send(new PortalAddCommentCommand(
            ticketId, GetCustomerId(), GetTenantId(), request.Content));
        return Ok(result);
    }

    [HttpGet("knowledge")]
    public async Task<ActionResult<PaginatedList<KnowledgeArticleDto>>> SearchKnowledge(
        [FromQuery] string? term, [FromQuery] string? categoryId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Reuse existing knowledge queries, filtered to published articles only.
        if (!string.IsNullOrWhiteSpace(term))
            return Ok(await _mediator.Send(new SearchKnowledgeArticlesQuery(term, page, pageSize)));

        return Ok(await _mediator.Send(new GetKnowledgeArticlesQuery(
            categoryId is not null ? Guid.Parse(categoryId) : null, true, page, pageSize)));
    }

    [HttpPost("chat/start")]
    public async Task<ActionResult<ConversationDto>> StartChat([FromBody] PortalStartChatRequest? request)
    {
        var result = await _mediator.Send(new CreateConversationCommand(
            GetCustomerId(), ChannelType.LiveChat, request?.Subject, null));
        return Ok(result);
    }

    [HttpGet("knowledge/{id:guid}")]
    public async Task<ActionResult<KnowledgeArticleDetailDto>> GetKnowledgeArticle(Guid id)
    {
        var result = await _mediator.Send(new GetKnowledgeArticleByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<PortalUserDto>> GetProfile()
    {
        var result = await _mediator.Send(new GetPortalProfileQuery(GetPortalUserId()));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<Result>> UpdateProfile(PortalUpdateProfileRequest request)
    {
        var result = await _mediator.Send(new PortalUpdateProfileCommand(
            GetPortalUserId(), request.FullName, request.FullNameAr, request.Phone, request.NewPassword));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

public record PortalAddCommentRequest(string Content);
public record PortalUpdateProfileRequest(string FullName, string FullNameAr, string? Phone, string? NewPassword);
public record PortalStartChatRequest(string? Subject);
