using Sabro.Shared.Results;

namespace Sabro.Historical.Domain;

/// <summary>
/// A short description of a figure in one language — a sentence or two of who
/// they were, shown when a Shmo round is revealed.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately shaped like <c>LexiconMeaning</c>: one row per language rather
/// than a column per language, so adding a sixth language is content, not a
/// migration.
/// </para>
/// <para>
/// Unlike a Lexicon meaning, a description does <b>not</b> gate publication. The
/// 289 seeded figures were published long before this field existed, and a
/// publish rule requiring descriptions would retroactively invalidate every one
/// of them. It is enrichment, in the same class as a pronunciation recording.
/// </para>
/// <para>
/// Reveal-only by design: a description names the person, so showing it during a
/// round would hand over the answer.
/// </para>
/// </remarks>
public sealed record HistoricalFigureDescription
{
    /// <summary>
    /// Long enough for two full sentences, short enough that the reveal card stays
    /// a card. Descriptions that want more room are telling you they belong in an
    /// article rather than on a game result.
    /// </summary>
    public const int MaxTextLength = 500;

    private HistoricalFigureDescription(string language, string text)
    {
        Language = language;
        Text = text;
    }

    /// <summary>Lowercase ISO code, e.g. <c>en</c>.</summary>
    public string Language { get; }

    public string Text { get; }

    public static Result<HistoricalFigureDescription> Create(string language, string text)
    {
        var trimmedLanguage = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedLanguage.Length == 0)
        {
            return Result<HistoricalFigureDescription>.Failure(Error.Validation("Language is required."));
        }

        if (!IsValidLanguageCode(trimmedLanguage))
        {
            return Result<HistoricalFigureDescription>.Failure(
                Error.Validation("Language must be a 2- or 3-letter ISO code."));
        }

        var trimmedText = (text ?? string.Empty).Trim();
        if (trimmedText.Length == 0)
        {
            return Result<HistoricalFigureDescription>.Failure(Error.Validation("Text is required."));
        }

        if (trimmedText.Length > MaxTextLength)
        {
            return Result<HistoricalFigureDescription>.Failure(
                Error.Validation($"Description must be {MaxTextLength} characters or fewer."));
        }

        return Result<HistoricalFigureDescription>.Success(
            new HistoricalFigureDescription(trimmedLanguage, trimmedText));
    }

    private static bool IsValidLanguageCode(string code)
    {
        if (code.Length is < 2 or > 3)
        {
            return false;
        }

        foreach (var ch in code)
        {
            if (ch is < 'a' or > 'z')
            {
                return false;
            }
        }

        return true;
    }
}
