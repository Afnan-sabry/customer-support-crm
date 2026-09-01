using CustomerSupport.Application.Roles.DTOs;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Roles.Queries;

public record GetPermissionsQuery : IRequest<List<PermissionDto>>;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<PermissionDto>>
{
    private readonly AppDbContext _context;

    public GetPermissionsQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Permissions
            .Select(p => new PermissionDto(p.Id, p.Key, p.Module, p.Description))
            .OrderBy(p => p.Module).ThenBy(p => p.Key)
            .ToListAsync(cancellationToken);
    }
}
