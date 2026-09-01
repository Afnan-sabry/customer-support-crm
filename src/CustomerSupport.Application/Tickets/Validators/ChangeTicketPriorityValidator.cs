using CustomerSupport.Application.Tickets.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Tickets.Validators;

public class ChangeTicketPriorityValidator : AbstractValidator<ChangeTicketPriorityCommand>
{
    public ChangeTicketPriorityValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.PriorityId).NotEmpty();
    }
}
