using CustomerSupport.Application.Users.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Users.Validators;

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FullNameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PreferredLanguage).Must(l => l is "en" or "ar");
    }
}
