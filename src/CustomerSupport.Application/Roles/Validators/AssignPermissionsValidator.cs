using CustomerSupport.Application.Roles.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Roles.Validators;

public class AssignPermissionsValidator : AbstractValidator<AssignPermissionsCommand>
{
    public AssignPermissionsValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionIds).NotNull();
    }
}
