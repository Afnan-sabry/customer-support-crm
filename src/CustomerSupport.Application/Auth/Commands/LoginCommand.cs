using CustomerSupport.Application.Auth.DTOs;
using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new LoginResponse(
            accessToken,
            refreshToken,
            new UserDto(user.Id, user.Email!, user.FullName, user.FullNameAr, user.TenantId, user.PreferredLanguage, roles.ToList()));
    }
}
