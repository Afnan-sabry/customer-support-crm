using CustomerSupport.Application.Assignment.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Assignment.Validators;

public class UpdateAssignmentRuleValidator : AbstractValidator<UpdateAssignmentRuleCommand>
{
    public UpdateAssignmentRuleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Strategy).NotEmpty().Must(s => s is "RoundRobin" or "LeastLoad")
            .WithMessage("Strategy must be 'RoundRobin' or 'LeastLoad'.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
