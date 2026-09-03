using CustomerSupport.Application.Sla.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Sla.Validators;

public class CreateSlaPolicyValidator : AbstractValidator<CreateSlaPolicyCommand>
{
    public CreateSlaPolicyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstResponseMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionMinutes).GreaterThanOrEqualTo(x => x.FirstResponseMinutes)
            .WithMessage("Resolution time must be >= first response time.");
    }
}
