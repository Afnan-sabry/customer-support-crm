using CustomerSupport.Application.Knowledge.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Knowledge.Validators;

public class CreateKnowledgeCategoryValidator : AbstractValidator<CreateKnowledgeCategoryCommand>
{
    public CreateKnowledgeCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
    }
}
