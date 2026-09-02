using CustomerSupport.Application.Conversations.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Conversations.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.ContentType).IsInEnum();
    }
}
