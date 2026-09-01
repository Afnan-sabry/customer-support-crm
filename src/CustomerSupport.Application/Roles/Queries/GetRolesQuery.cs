using CustomerSupport.Application.Roles.DTOs;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Roles.Queries;

public record GetRolesQuery : IRequest<List<RoleDto>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRolesQueryHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Roles
            .Where(r => r.TenantId == _currentUserService.TenantId)
            .Select(r => new RoleDto(
                r.Id,
                r.Name!,
                r.NameAr,
                r.IsSystem,
                _context.RolePermissions
                    .Where(rp => rp.RoleId == r.Id)
                    .Select(rp => new PermissionDto(
                        rp.Permission!.Id, rp.Permission.Key, rp.Permission.Module, rp.Permission.Description))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
