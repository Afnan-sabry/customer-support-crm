using CustomerSupport.Application.Roles.DTOs;
using CustomerSupport.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Roles.Commands;

public record UpdateRoleCommand(Guid RoleId, string Name, string NameAr) : IRequest<RoleDto>;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UpdateRoleCommandHandler(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId.ToString())
            ?? throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be modified.");

        role.Name = request.Name;
        role.NameAr = request.NameAr;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new RoleDto(role.Id, role.Name!, role.NameAr, role.IsSystem, []);
    }
}
