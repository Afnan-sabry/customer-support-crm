using CustomerSupport.Application.Common.Models;
using CustomerSupport.Application.Users.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Users.Queries;

public record GetUsersQuery(string? Search, int Page = 1, int PageSize = 20) : IRequest<PaginatedList<UserDetailDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDetailDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly AppDbContext _context;

    public GetUsersQueryHandler(UserManager<ApplicationUser> userManager, ICurrentUserService currentUserService, AppDbContext context)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _context = context;
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

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await _context.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);
        var rolesByUser = userRoles.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

        var dtos = users.Select(user => new UserDetailDto(
            user.Id, user.Email!, user.FullName, user.FullNameAr, user.Phone,
            user.TenantId, user.PreferredLanguage, user.IsActive,
            rolesByUser.GetValueOrDefault(user.Id, []))).ToList();

        return new PaginatedList<UserDetailDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
