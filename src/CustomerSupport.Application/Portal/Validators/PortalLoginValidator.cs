using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalLoginValidator : AbstractValidator<PortalLoginCommand>
{
    public PortalLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
