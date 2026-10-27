using Sabro.Shared.Results;

namespace Sabro.BethGazo.Domain;

/// <summary>
/// One language's line about a section or a mode, shown in the hub's Beth Gazo:
/// for a section, what it is and when it is sung; for a mode, its character and
/// how to tell it apart (Owner, 2026-10-25). Optional, one per language.
/// </summary>
public sealed record BethGazoDescription
{
    /// <summary>A line, not an essay: the book page shows it under a heading.</summary>
    public const int MaxTextLength = 300;

    private BethGazoDescription(string language, string text)
    {
        Language = language;
        Text = text;
    }

    /// <summary>ISO 639-1/639-3 code, lower-case ("en", "fr", "syc").</summary>
    public string Language { get; }

    public string Text { get; }

    public static Result<BethGazoDescription> Create(string language, string text)
    {
        var trimmedLanguage = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedLanguage.Length is < 2 or > 3 || trimmedLanguage.Any(ch => ch is < 'a' or > 'z'))
        {
            return Result<BethGazoDescription>.Failure(Error.Validation("Language must be a 2- or 3-letter ISO code."));
        }

        var trimmedText = (text ?? string.Empty).Trim();
        if (trimmedText.Length == 0)
        {
            return Result<BethGazoDescription>.Failure(Error.Validation("Text is required."));
        }

        if (trimmedText.Length > MaxTextLength)
        {
            return Result<BethGazoDescription>.Failure(
                Error.Validation($"A description must be {MaxTextLength} characters or fewer."));
        }

        return Result<BethGazoDescription>.Success(new BethGazoDescription(trimmedLanguage, trimmedText));
    }
}
