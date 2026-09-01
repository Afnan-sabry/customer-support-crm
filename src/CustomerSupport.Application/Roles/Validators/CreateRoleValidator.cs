using CustomerSupport.Application.Roles.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Roles.Validators;

public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(100);
    }
}
