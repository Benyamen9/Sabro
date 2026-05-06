using FluentValidation;

namespace Sabro.Lexicon.Application.Roots;

public sealed class CreateLexiconRootRequestValidator : AbstractValidator<CreateLexiconRootRequest>
{
    public CreateLexiconRootRequestValidator()
    {
        RuleFor(x => x.Form)
            .NotEmpty()
            .MaximumLength(32);
    }
}
