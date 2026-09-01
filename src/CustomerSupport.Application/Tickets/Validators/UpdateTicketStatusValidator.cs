using CustomerSupport.Application.Tickets.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Tickets.Validators;

public class UpdateTicketStatusValidator : AbstractValidator<UpdateTicketStatusCommand>
{
    public UpdateTicketStatusValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.StatusId).NotEmpty();
    }
}
