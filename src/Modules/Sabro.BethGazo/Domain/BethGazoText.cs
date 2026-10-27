using System.Text;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.BethGazo.Domain;

/// <summary>
/// The rules shared by a section's and a mode's descriptive text: their Syriac
/// name and their one-line descriptions.
/// </summary>
internal static class BethGazoText
{
    public const int MaxSyriacNameLength = 64;

    /// <summary>Blank means "none yet"; anything else must be Syriac script, stored NFC.</summary>
    public static Result<string?> NormalizeSyriacName(string? syriacName)
    {
        var trimmed = syriacName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Result<string?>.Success(null);
        }

        var normalized = trimmed.Normalize(NormalizationForm.FormC);
        if (normalized.Length > MaxSyriacNameLength)
        {
            return Result<string?>.Failure(
                Error.Validation($"Syriac name must be at most {MaxSyriacNameLength} characters."));
        }

        return SyriacText.IsSyriacOnly(normalized)
            ? Result<string?>.Success(normalized)
            : Result<string?>.Failure(Error.Validation("Syriac name must be written in Syriac script."));
    }

    /// <summary>
    /// Rejects two descriptions in one language rather than keeping one: the page
    /// picks by language, so a duplicate would be a coin toss over which is shown.
    /// </summary>
    public static Error? CheckDistinctLanguages(IReadOnlyCollection<BethGazoDescription> descriptions)
    {
        var duplicate = descriptions
            .GroupBy(d => d.Language, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        return duplicate is null
            ? null
            : Error.Validation($"Only one description per language is allowed (got several for '{duplicate.Key}').");
    }
}
