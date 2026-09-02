using CustomerSupport.Application.Portal.Commands;
using CustomerSupport.Application.Portal.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.API.Controllers;

[ApiController]
[Route("api/v1/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPortalTokenService _portalTokenService;
    private readonly AppDbContext _context;

    public PortalAuthController(IMediator mediator, IPortalTokenService portalTokenService, AppDbContext context)
    {
        _mediator = mediator;
        _portalTokenService = portalTokenService;
        _context = context;
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

    [HttpPost("refresh")]
    public async Task<ActionResult<PortalTokenResponse>> Refresh(PortalRefreshRequest request)
    {
        var principal = _portalTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var portalUserIdClaim = principal?.FindFirst("PortalUserId")?.Value;

        if (portalUserIdClaim is null || !Guid.TryParse(portalUserIdClaim, out var portalUserId))
            return Unauthorized("Invalid access token");

        var user = await _context.PortalUsers.FirstOrDefaultAsync(u => u.Id == portalUserId);

        if (user is null
            || user.RefreshToken != request.RefreshToken
            || user.RefreshTokenExpiryTime is null
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized("Invalid or expired refresh token");
        }

        var (accessToken, refreshToken) = _portalTokenService.GenerateTokens(user);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new PortalTokenResponse(accessToken, refreshToken,
            new PortalUserDto(user.Id, user.Email, user.FullName, user.FullNameAr, user.Phone, user.CustomerId)));
    }
}

public record PortalRefreshRequest(string AccessToken, string RefreshToken);
