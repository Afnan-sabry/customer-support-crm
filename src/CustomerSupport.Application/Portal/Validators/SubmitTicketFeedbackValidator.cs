using CustomerSupport.Application.Portal.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Portal.Validators;

public class SubmitTicketFeedbackValidator : AbstractValidator<SubmitTicketFeedbackCommand>
{
    public SubmitTicketFeedbackValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
