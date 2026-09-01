using CustomerSupport.Application.Roles.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Roles.Queries;

public record GetRolesQuery : IRequest<List<RoleDto>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly AppDbContext _context;

    public GetRolesQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Roles
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
