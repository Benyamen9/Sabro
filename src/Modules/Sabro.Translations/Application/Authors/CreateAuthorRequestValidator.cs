using FluentValidation;

namespace Sabro.Translations.Application.Authors;

public sealed class CreateAuthorRequestValidator : AbstractValidator<CreateAuthorRequest>
{
    public CreateAuthorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.SyriacName)
            .MaximumLength(256)
            .When(x => x.SyriacName is not null);

        RuleFor(x => x.Title)
            .MaximumLength(256)
            .When(x => x.Title is not null);
    }
}
