using FluentValidation;
using Sabro.Identity.Domain;

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

        RuleFor(x => x.DisplayName)
            .MaximumLength(UserProfile.MaxDisplayNameLength)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));

        // Appearing on the leaderboard needs a label to show.
        RuleFor(x => x.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .When(x => x.ShowOnLeaderboard)
            .WithMessage("A display name is required to appear on the leaderboard.");
    }
}
