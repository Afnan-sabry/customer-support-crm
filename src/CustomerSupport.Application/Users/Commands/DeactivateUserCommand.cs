using CustomerSupport.Application.Common.Models;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Users.Commands;

public record DeactivateUserCommand(Guid Id) : IRequest<Result>;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public DeactivateUserCommandHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null || user.TenantId != _currentUserService.TenantId) return Result.Failure("User not found.");

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }
}
