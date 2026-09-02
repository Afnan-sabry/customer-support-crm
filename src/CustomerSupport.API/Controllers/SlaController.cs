using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Sla.Commands;
using CustomerSupport.Application.Sla.DTOs;
using CustomerSupport.Application.Sla.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SlaController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlaController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:sla.view")]
    public async Task<ActionResult<PaginatedList<SlaPolicyDto>>> GetAll(
        [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _mediator.Send(new GetSlaPoliciesQuery(isActive, page, pageSize)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:sla.view")]
    public async Task<ActionResult<SlaPolicyDto>> GetById(Guid id)
        => Ok(await _mediator.Send(new GetSlaPolicyByIdQuery(id)));

    [HttpPost]
    [Authorize(Policy = "Permission:sla.manage")]
    public async Task<ActionResult<SlaPolicyDto>> Create(CreateSlaPolicyCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:sla.manage")]
    public async Task<ActionResult<SlaPolicyDto>> Update(Guid id, UpdateSlaPolicyCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:sla.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteSlaPolicyCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
