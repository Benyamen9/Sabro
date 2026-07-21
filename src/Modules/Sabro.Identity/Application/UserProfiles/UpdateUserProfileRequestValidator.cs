using FluentValidation;
using Microsoft.Extensions.Options;
using Sabro.Identity.Domain;
using Sabro.Shared.Localization;

namespace Sabro.Identity.Application.UserProfiles;

public sealed class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator(IOptions<SupportedLanguagesOptions> supportedLanguages)
    {
        var codes = supportedLanguages.Value.Codes;

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .Must(language => codes.Contains(language?.Trim().ToLowerInvariant()))
            .WithMessage($"PreferredLanguage must be one of: {string.Join(", ", codes)}.");

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
