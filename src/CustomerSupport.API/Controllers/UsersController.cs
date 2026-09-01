using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Users.Commands;
using CustomerSupport.Application.Users.DTOs;
using CustomerSupport.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:users.view")]
    public async Task<ActionResult<PaginatedList<UserDetailDto>>> GetUsers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _mediator.Send(new GetUsersQuery(search, page, pageSize)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:users.view")]
    public async Task<ActionResult<UserDetailDto>> GetUser(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:users.create")]
    public async Task<ActionResult<Result>> CreateUser(CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:users.edit")]
    public async Task<ActionResult<Result>> UpdateUser(Guid id, UpdateUserCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:users.deactivate")]
    public async Task<ActionResult<Result>> DeactivateUser(Guid id)
    {
        var result = await _mediator.Send(new DeactivateUserCommand(id));
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
