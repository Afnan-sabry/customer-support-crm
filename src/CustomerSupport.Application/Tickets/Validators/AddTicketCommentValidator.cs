using CustomerSupport.Application.Tickets.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Tickets.Validators;

public class AddTicketCommentValidator : AbstractValidator<AddTicketCommentCommand>
{
    public AddTicketCommentValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}
