using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Escalation.Commands;
using CustomerSupport.Application.Escalation.DTOs;
using CustomerSupport.Application.Escalation.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/escalation-rules")]
public class EscalationRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EscalationRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "Permission:escalation.view")]
    public async Task<ActionResult<List<EscalationRuleDto>>> GetAll([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetEscalationRulesQuery(isActive)));

    [HttpPost]
    [Authorize(Policy = "Permission:escalation.manage")]
    public async Task<ActionResult<EscalationRuleDto>> Create(CreateEscalationRuleCommand command)
        => CreatedAtAction(null, await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:escalation.manage")]
    public async Task<ActionResult<EscalationRuleDto>> Update(Guid id, UpdateEscalationRuleCommand command)
    {
        if (id != command.Id) return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:escalation.manage")]
    public async Task<ActionResult<Result>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteEscalationRuleCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
