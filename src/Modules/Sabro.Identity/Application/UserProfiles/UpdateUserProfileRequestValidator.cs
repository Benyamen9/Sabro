using FluentValidation;

namespace Sabro.Identity.Application.UserProfiles;

public sealed class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    private static readonly string[] SupportedLanguages = { "en", "fr", "nl" };

    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .Must(language => SupportedLanguages.Contains(language?.Trim().ToLowerInvariant()))
            .WithMessage("PreferredLanguage must be one of: en, fr, nl.");

        RuleFor(x => x.PreferredScriptVariant)
            .IsInEnum();
    }
}
