using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class PortalAddCommentValidator : AbstractValidator<PortalAddCommentCommand>
{
    public PortalAddCommentValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}
