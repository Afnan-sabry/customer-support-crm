using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Users.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Users.Queries;

public record GetUsersQuery(string? Search, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<UserDetailDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDetailDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public GetUsersQueryHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<UserDetailDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.Where(u => u.TenantId == _currentUserService.TenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(search) ||
                u.FullNameAr.Contains(search) ||
                u.Email!.ToLower().Contains(search));
        }

        query = query.OrderBy(u => u.FullName);
        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        var dtos = new List<UserDetailDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UserDetailDto(
                user.Id, user.Email!, user.FullName, user.FullNameAr, user.Phone,
                user.TenantId, user.PreferredLanguage, user.IsActive, roles.ToList()));
        }

        return new PaginatedList<UserDetailDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
