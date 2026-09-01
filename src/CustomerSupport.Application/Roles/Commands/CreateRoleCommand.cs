using CustomerSupport.Application.Roles.DTOs;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CustomerSupport.Application.Roles.Commands;

public record CreateRoleCommand(string Name, string NameAr) : IRequest<RoleDto>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ICurrentUserService _currentUserService;

    public CreateRoleCommandHandler(RoleManager<ApplicationRole> roleManager, ICurrentUserService currentUserService)
    {
        _roleManager = roleManager;
        _currentUserService = currentUserService;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new ApplicationRole
        {
            Name = request.Name,
            NameAr = request.NameAr,
            TenantId = _currentUserService.TenantId,
            IsSystem = false
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new RoleDto(role.Id, role.Name!, role.NameAr, role.IsSystem, []);
    }
}
