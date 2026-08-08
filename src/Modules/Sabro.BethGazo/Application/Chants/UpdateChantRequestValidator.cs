using FluentValidation;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Application.Chants;

public sealed class UpdateChantRequestValidator : AbstractValidator<UpdateChantRequest>
{
    public UpdateChantRequestValidator()
    {
        RuleFor(x => x.SyriacIncipit).NotEmpty().MaximumLength(512);
        RuleFor(x => x.SyriacIncipitVocalized).MaximumLength(512);
        RuleFor(x => x.Transliteration).NotEmpty().MaximumLength(Chant.MaxTransliterationLength);
        RuleFor(x => x.SectionId).NotEmpty().WithMessage("A section is required.");

        // No rule on ModeId — see CreateChantRequestValidator.
    }
}
