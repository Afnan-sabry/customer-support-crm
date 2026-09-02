using CustomerSupport.Application.Escalation.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Escalation.Validators;

public class UpdateEscalationRuleValidator : AbstractValidator<UpdateEscalationRuleCommand>
{
    public UpdateEscalationRuleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TriggerType).NotEmpty().Must(t => t is "FirstResponseBreached" or "ResolutionBreached")
            .WithMessage("TriggerType must be 'FirstResponseBreached' or 'ResolutionBreached'.");
        RuleFor(x => x.TriggerAfterMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActionType).NotEmpty().Must(a => a is "Reassign" or "ChangePriority")
            .WithMessage("ActionType must be 'Reassign' or 'ChangePriority'.");
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
