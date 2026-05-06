using FluentValidation;

namespace Sabro.Lexicon.Application.Entries;

public sealed class CreateLexiconMeaningRequestValidator : AbstractValidator<CreateLexiconMeaningRequest>
{
    public CreateLexiconMeaningRequestValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.Text)
            .NotEmpty();
    }
}
