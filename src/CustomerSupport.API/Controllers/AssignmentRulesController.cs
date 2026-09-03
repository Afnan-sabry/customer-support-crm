using CustomerSupport.Application.Assignment.Commands;
using CustomerSupport.Application.Assignment.DTOs;
using CustomerSupport.Application.Assignment.Queries;
using CustomerSupport.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/assignment-rules")]
public class AssignmentRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssignmentRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:assignment.view")]
    public async Task<ActionResult<List<AssignmentRuleDto>>> GetAll([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetAssignmentRulesQuery(isActive)));

    [HttpPost]
    [Authorize(Policy = "Permission:assignment.manage")]
    public async Task<ActionResult<AssignmentRuleDto>> Create(CreateAssignmentRuleCommand command)
        => CreatedAtAction(null, await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:assignment.manage")]
    public async Task<ActionResult<AssignmentRuleDto>> Update(Guid id, UpdateAssignmentRuleCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:assignment.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteAssignmentRuleCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
