using System.Linq;
using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Application.Search;

internal static class LexiconEntryDocumentMapper
{
    public static LexiconEntrySearchDocument Map(LexiconEntry entry, string? rootForm) => new()
    {
        Id = entry.Id.ToString("D"),
        SyriacUnvocalized = entry.SyriacUnvocalized,
        SyriacVocalized = entry.SyriacVocalized,
        SblTransliteration = entry.SblTransliteration ?? string.Empty,
        TransliterationVariants = entry.TransliterationVariants.ToArray(),
        RootId = entry.RootId?.ToString("D"),
        RootForm = rootForm,
        GrammaticalCategory = entry.GrammaticalCategory.ToString(),
        Morphology = entry.Morphology,
        MeaningTexts = entry.Meanings.Select(m => m.Text).ToArray(),
        MeaningLanguages = entry.Meanings.Select(m => m.Language).Distinct().ToArray(),
        Status = entry.Status.ToString(),
        PlayableInMeltho = entry.PlayableInMeltho,
        PlayableLength = entry.PlayableLength,
        CreatedAtUnix = entry.CreatedAt.ToUnixTimeSeconds(),
    };
}
