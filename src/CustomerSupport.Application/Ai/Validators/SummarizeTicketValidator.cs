using CustomerSupport.Application.Ai.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Ai.Validators;

public class SummarizeTicketValidator : AbstractValidator<SummarizeTicketCommand>
{
    public SummarizeTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
