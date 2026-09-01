using CustomerSupport.Application.Tickets.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Tickets.Validators;

public class AddTicketAttachmentValidator : AbstractValidator<AddTicketAttachmentCommand>
{
    public AddTicketAttachmentValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FilePath).NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileSize).GreaterThan(0);
    }
}
