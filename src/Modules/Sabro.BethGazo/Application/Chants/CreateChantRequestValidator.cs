using FluentValidation;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

public sealed class CreateChantRequestValidator : AbstractValidator<CreateChantRequest>
{
    public CreateChantRequestValidator()
    {
        // Optional (owner, 2026-10-15): a chant can go live from its recording
        // alone. When given, the domain still requires Syriac characters only.
        RuleFor(x => x.SyriacIncipit).MaximumLength(512);
        RuleFor(x => x.SyriacIncipitVocalized).MaximumLength(512);

        // Required, unlike the Lexicon's transliteration: this is the name a player
        // types, not a search aid.
        RuleFor(x => x.Transliteration).NotEmpty().MaximumLength(Chant.MaxTransliterationLength);

        RuleFor(x => x.SectionId).NotEmpty().WithMessage("A section is required.");

        // Deliberately NOT NotEmpty: whether a mode is required depends on the
        // section, which this validator cannot see. BethGazoSection.ValidateMode
        // owns that rule in both directions, so asserting it here too would either
        // duplicate it or contradict it.
    }
}
