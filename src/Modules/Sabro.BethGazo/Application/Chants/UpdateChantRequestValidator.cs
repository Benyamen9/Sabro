using FluentValidation;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

public sealed class UpdateChantRequestValidator : AbstractValidator<UpdateChantRequest>
{
    public UpdateChantRequestValidator()
    {
        // Optional (owner, 2026-10-15): a chant can go live from its recording
        // alone. When given, the domain still requires Syriac characters only.
        RuleFor(x => x.SyriacIncipit).MaximumLength(512);
        RuleFor(x => x.SyriacIncipitVocalized).MaximumLength(512);
        RuleFor(x => x.Transliteration).NotEmpty().MaximumLength(Chant.MaxTransliterationLength);
        RuleFor(x => x.SectionId).NotEmpty().WithMessage("A section is required.");

        // No rule on ModeId — see CreateChantRequestValidator.
    }
}
