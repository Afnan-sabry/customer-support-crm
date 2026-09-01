using CustomerSupport.Application.Tickets.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Tickets.Validators;

public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.PriorityId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty();
    }
}
