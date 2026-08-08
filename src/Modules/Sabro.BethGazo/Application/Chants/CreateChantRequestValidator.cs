using FluentValidation;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

public sealed class CreateChantRequestValidator : AbstractValidator<CreateChantRequest>
{
    public CreateChantRequestValidator()
    {
        RuleFor(x => x.SyriacIncipit).NotEmpty().MaximumLength(512);
        RuleFor(x => x.SyriacIncipitVocalized).MaximumLength(512);

        // Required, unlike the Lexicon's transliteration: this is the name a player
        // types, not a search aid.
        RuleFor(x => x.Transliteration).NotEmpty().MaximumLength(Chant.MaxTransliterationLength);

        RuleFor(x => x.Shuhlofo).MaximumLength(Chant.MaxShuhlofoLength);

        RuleFor(x => x.SectionId).NotEmpty().WithMessage("A section is required.");

        // Deliberately NOT NotEmpty: whether a mode is required depends on the
        // section, which this validator cannot see. BethGazoSection.ValidateMode
        // owns that rule in both directions, so asserting it here too would either
        // duplicate it or contradict it.
    }
}
