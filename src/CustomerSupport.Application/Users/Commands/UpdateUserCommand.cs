using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Users.Commands;

public record UpdateUserCommand(
    Guid Id,
    string FullName,
    string FullNameAr,
    string? Phone,
    string PreferredLanguage,
    List<string> Roles) : IRequest<Result>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public UpdateUserCommandHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null || user.TenantId != _currentUserService.TenantId) return Result.Failure("User not found.");

        user.FullName = request.FullName;
        user.FullNameAr = request.FullNameAr;
        user.Phone = request.Phone;
        user.PreferredLanguage = request.PreferredLanguage;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Failure(updateResult.Errors.Select(e => e.Description).ToArray());

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (request.Roles.Count > 0)
            await _userManager.AddToRolesAsync(user, request.Roles);

        return Result.Success();
    }
}
