using CustomerSupport.Application.Tickets.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Tickets.Validators;

public class AssignTicketValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
