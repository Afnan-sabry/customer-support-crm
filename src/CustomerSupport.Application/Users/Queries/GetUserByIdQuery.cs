using CustomerSupport.Application.Users.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDetailDto?>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public GetUserByIdQueryHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<UserDetailDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user is null || user.TenantId != _currentUserService.TenantId) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDetailDto(
            user.Id, user.Email!, user.FullName, user.FullNameAr, user.Phone,
            user.TenantId, user.PreferredLanguage, user.IsActive, roles.ToList());
    }
}
