using CustomerSupport.Application.Conversations.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Conversations.Validators;

public class CreateConversationValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Subject).MaximumLength(500);
        RuleFor(x => x.ExternalReference).MaximumLength(500);
    }
}
