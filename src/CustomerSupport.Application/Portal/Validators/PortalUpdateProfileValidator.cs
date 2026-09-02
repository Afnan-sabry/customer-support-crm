using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalUpdateProfileValidator : AbstractValidator<PortalUpdateProfileCommand>
{
    public PortalUpdateProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NewPassword).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.NewPassword));
    }
}
