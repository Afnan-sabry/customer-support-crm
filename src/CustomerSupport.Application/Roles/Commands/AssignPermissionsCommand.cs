using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerSupport.Application.Roles.Commands;

public record AssignPermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest;

public class AssignPermissionsCommandHandler : IRequestHandler<AssignPermissionsCommand>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AssignPermissionsCommandHandler(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FindAsync([request.RoleId], cancellationToken)
            ?? throw new KeyNotFoundException("Role not found.");
        if (role.TenantId != _currentUserService.TenantId)
            throw new KeyNotFoundException("Role not found.");

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
