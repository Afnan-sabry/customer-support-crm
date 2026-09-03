using CustomerSupport.Application.Ai.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Ai.Validators;

public class CategorizeTicketValidator : AbstractValidator<CategorizeTicketCommand>
{
    public CategorizeTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
