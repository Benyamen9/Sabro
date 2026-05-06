using FluentValidation;

namespace Sabro.Lexicon.Application.Entries;

public sealed class CreateLexiconEntryRequestValidator : AbstractValidator<CreateLexiconEntryRequest>
{
    public CreateLexiconEntryRequestValidator()
    {
        RuleFor(x => x.SyriacUnvocalized)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.SyriacVocalized)
            .MaximumLength(256)
            .When(x => x.SyriacVocalized is not null);

        RuleFor(x => x.SblTransliteration)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.GrammaticalCategory)
            .IsInEnum();

        RuleFor(x => x.RootId!.Value)
            .NotEqual(Guid.Empty)
            .When(x => x.RootId.HasValue);

        RuleForEach(x => x.TransliterationVariants)
            .MaximumLength(128)
            .When(x => x.TransliterationVariants is not null);

        RuleForEach(x => x.Meanings!)
            .SetValidator(new CreateLexiconMeaningRequestValidator())
            .When(x => x.Meanings is not null);
    }
}
