using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalSubmitTicketValidator : AbstractValidator<PortalSubmitTicketCommand>
{
    public PortalSubmitTicketValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.PriorityId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty();
    }
}
