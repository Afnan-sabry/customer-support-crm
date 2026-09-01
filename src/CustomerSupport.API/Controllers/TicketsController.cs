using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Tickets.Commands;
using CustomerSupport.Application.Tickets.DTOs;
using CustomerSupport.Application.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:tickets.view")]
    public async Task<ActionResult<PaginatedList<TicketDto>>> GetTickets(
        [FromQuery] Guid? statusId, [FromQuery] Guid? priorityId,
        [FromQuery] Guid? categoryId, [FromQuery] Guid? assignedToId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetTicketsQuery(statusId, priorityId, categoryId, assignedToId, search, page, pageSize)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:tickets.view")]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(Guid id)
    {
        var result = await _mediator.Send(new GetTicketByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:tickets.create")]
    public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetTicket), new { id = result.Id }, result);
    }

    [HttpPut("{ticketId:guid}/status")]
    [Authorize(Policy = "Permission:tickets.edit")]
    public async Task<ActionResult<Result>> UpdateStatus(Guid ticketId, UpdateTicketStatusCommand command)
    {
        if (ticketId != command.TicketId) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{ticketId:guid}/priority")]
    [Authorize(Policy = "Permission:tickets.edit")]
    public async Task<ActionResult<Result>> ChangePriority(Guid ticketId, ChangeTicketPriorityCommand command)
    {
        if (ticketId != command.TicketId) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{ticketId:guid}/assign")]
    [Authorize(Policy = "Permission:tickets.assign")]
    public async Task<ActionResult<Result>> AssignTicket(Guid ticketId, AssignTicketCommand command)
    {
        if (ticketId != command.TicketId) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{ticketId:guid}/comments")]
    [Authorize(Policy = "Permission:tickets.edit")]
    public async Task<ActionResult<TicketCommentDto>> AddComment(Guid ticketId, AddTicketCommentCommand command)
    {
        if (ticketId != command.TicketId) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("{ticketId:guid}/attachments")]
    [Authorize(Policy = "Permission:tickets.edit")]
    public async Task<ActionResult<TicketAttachmentDto>> AddAttachment(Guid ticketId, AddTicketAttachmentCommand command)
    {
        if (ticketId != command.TicketId) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<TicketCategoryDto>>> GetCategories()
        => Ok(await _mediator.Send(new GetTicketCategoriesQuery()));

    [HttpGet("priorities")]
    public async Task<ActionResult<List<TicketPriorityDto>>> GetPriorities()
        => Ok(await _mediator.Send(new GetTicketPrioritiesQuery()));

    [HttpGet("statuses")]
    public async Task<ActionResult<List<TicketStatusDto>>> GetStatuses()
        => Ok(await _mediator.Send(new GetTicketStatusesQuery()));
}
