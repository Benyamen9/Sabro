using Sabro.Shared.Results;

namespace Sabro.Lexicon.Domain;

public sealed record LexiconMeaning
{
    private LexiconMeaning(string language, string text)
    {
        Language = language;
        Text = text;
    }

    public string Language { get; }

    public string Text { get; }

    public static Result<LexiconMeaning> Create(string language, string text)
    {
        var trimmedLanguage = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedLanguage.Length == 0)
        {
            return Result<LexiconMeaning>.Failure(Error.Validation("Language is required."));
        }

        if (!IsValidLanguageCode(trimmedLanguage))
        {
            return Result<LexiconMeaning>.Failure(Error.Validation("Language must be a 2- or 3-letter ISO code."));
        }

        var trimmedText = (text ?? string.Empty).Trim();
        if (trimmedText.Length == 0)
        {
            return Result<LexiconMeaning>.Failure(Error.Validation("Text is required."));
        }

        return Result<LexiconMeaning>.Success(new LexiconMeaning(trimmedLanguage, trimmedText));
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
