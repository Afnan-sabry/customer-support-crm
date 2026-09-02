using CustomerSupport.Application.Portal.Commands;
using CustomerSupport.Application.Portal.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<PortalTokenResponse>> Register(PortalRegisterRequest request)
    {
        var result = await _mediator.Send(new PortalRegisterCommand(
            request.Email, request.Password, request.FullName, request.FullNameAr, request.Phone));
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<PortalTokenResponse>> Login(PortalLoginRequest request)
    {
        var result = await _mediator.Send(new PortalLoginCommand(request.Email, request.Password));
        return Ok(result);
    }
}
