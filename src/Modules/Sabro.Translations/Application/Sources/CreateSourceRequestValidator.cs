using FluentValidation;

namespace Sabro.Translations.Application.Sources;

public sealed class CreateSourceRequestValidator : AbstractValidator<CreateSourceRequest>
{
    public CreateSourceRequestValidator()
    {
        RuleFor(x => x.AuthorId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.OriginalLanguageCode)
            .MaximumLength(16)
            .When(x => x.OriginalLanguageCode is not null);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);
    }
}
