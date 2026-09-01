using CustomerSupport.Domain.Entities;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Roles.Commands;

public record AssignPermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest;

public class AssignPermissionsCommandHandler : IRequestHandler<AssignPermissionsCommand>
{
    private readonly AppDbContext _context;

    public AssignPermissionsCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.RolePermissions
            .Where(rp => rp.RoleId == request.RoleId)
            .ToListAsync(cancellationToken);

        _context.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in request.PermissionIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = request.RoleId,
                PermissionId = permissionId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
