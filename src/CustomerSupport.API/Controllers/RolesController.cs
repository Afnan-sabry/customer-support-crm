using CustomerSupport.Application.Roles.Commands;
using CustomerSupport.Application.Roles.DTOs;
using CustomerSupport.Application.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:roles.view")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        return Ok(await _mediator.Send(new GetRolesQuery()));
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "Permission:roles.view")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        return Ok(await _mediator.Send(new GetPermissionsQuery()));
    }

    [HttpPost]
    [Authorize(Policy = "Permission:roles.manage")]
    public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetRoles), result);
    }

    [HttpPut("{roleId:guid}")]
    [Authorize(Policy = "Permission:roles.manage")]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid roleId, UpdateRoleCommand command)
    {
        if (roleId != command.RoleId)
            return BadRequest();
        return Ok(await _mediator.Send(command));
    }

    [HttpPost("{roleId:guid}/permissions")]
    [Authorize(Policy = "Permission:roles.manage")]
    public async Task<IActionResult> AssignPermissions(Guid roleId, AssignPermissionsCommand command)
    {
        if (roleId != command.RoleId)
            return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }
}
