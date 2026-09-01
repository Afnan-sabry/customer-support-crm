using CustomerSupport.Application.Users.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Users.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredLanguage).Must(l => l is "en" or "ar");
    }
}
