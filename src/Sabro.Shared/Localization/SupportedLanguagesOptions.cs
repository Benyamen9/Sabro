namespace Sabro.Shared.Localization;

/// <summary>
/// The language codes the ecosystem currently supports for content and UI
/// preferences (Lexicon meanings required to publish, a user's preferred
/// language, and any future use), bound from the <c>SupportedLanguages</c>
/// section. Adding a language is a config change here — never a hardcoded
/// list duplicated per module, which drifts out of sync (as happened before:
/// the Lexicon publish gate and the profile validator each kept their own
/// copy, and only one was updated when German/Swedish were added).
/// </summary>
public sealed class SupportedLanguagesOptions
{
    public const string SectionName = "SupportedLanguages";

    public string[] Codes { get; set; } = ["en", "fr", "nl", "de", "sv"];
}
