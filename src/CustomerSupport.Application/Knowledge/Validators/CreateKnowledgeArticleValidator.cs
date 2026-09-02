using CustomerSupport.Application.Knowledge.Commands;
using FluentValidation;

namespace CustomerSupport.Application.Knowledge.Validators;

public class CreateKnowledgeArticleValidator : AbstractValidator<CreateKnowledgeArticleCommand>
{
    public CreateKnowledgeArticleValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Tags).MaximumLength(2000);
    }
}
