using CustomerSupport.Application.Auth.Commands;
using CustomerSupport.Application.Auth.DTOs;
using CustomerSupport.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<Result>> Register(RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Succeeded)
            return BadRequest(result);
        return Ok(result);
    }
}
