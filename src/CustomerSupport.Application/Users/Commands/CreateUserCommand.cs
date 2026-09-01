using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Users.Commands;

public record CreateUserCommand(
    string Email,
    string Password,
    string FullName,
    string FullNameAr,
    string? Phone,
    string PreferredLanguage,
    List<string> Roles) : IRequest<Result>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public CreateUserCommandHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            FullNameAr = request.FullNameAr,
            Phone = request.Phone,
            PreferredLanguage = request.PreferredLanguage,
            TenantId = _currentUserService.TenantId
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Result.Failure(createResult.Errors.Select(e => e.Description).ToArray());

        if (request.Roles.Count > 0)
            await _userManager.AddToRolesAsync(user, request.Roles);

        return Result.Success();
    }
}
