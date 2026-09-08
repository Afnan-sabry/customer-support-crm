using CustomerSupport.Application.Integrations.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Integrations.Validators;

public class CreateWebhookSubscriptionValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    private static readonly string[] ValidEvents =
        ["ticket.created", "ticket.status_changed", "ticket.resolved", "ticket.escalated",
         "sla.breached", "conversation.created", "conversation.closed"];

    public CreateWebhookSubscriptionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("URL must be a valid HTTPS URL.");
        RuleFor(x => x.Secret).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Events).NotEmpty()
            .Must(events => events.All(e => ValidEvents.Contains(e)))
            .WithMessage($"Events must be from: {string.Join(", ", ValidEvents)}");
    }
}
