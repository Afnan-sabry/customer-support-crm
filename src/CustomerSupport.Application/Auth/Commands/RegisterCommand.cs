using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string FullNameAr,
    Guid TenantId) : IRequest<Result>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            FullNameAr = request.FullNameAr,
            TenantId = request.TenantId
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);

        return identityResult.Succeeded
            ? Result.Success()
            : Result.Failure(identityResult.Errors.Select(e => e.Description).ToArray());
    }
}
