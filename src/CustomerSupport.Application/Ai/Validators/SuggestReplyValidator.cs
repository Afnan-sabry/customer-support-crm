using CustomerSupport.Application.Ai.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Ai.Validators;

public class SuggestReplyValidator : AbstractValidator<SuggestReplyCommand>
{
    public SuggestReplyValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
