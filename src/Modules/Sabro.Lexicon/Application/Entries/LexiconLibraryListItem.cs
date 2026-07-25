namespace Sabro.Lexicon.Application.Entries;

/// <summary>
/// Projection of a lexicon entry for the Meltho library list: the word, its glosses, and the
/// few enrichment fields the list surface sorts and renders by (transliteration, playable length,
/// grammatical category — a plain string, same wire convention as <c>DictionaryEntryListItem</c>).
/// </summary>
public sealed record LexiconLibraryListItem(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    int PlayableLength,
    IReadOnlyList<LexiconMeaningDto> Meanings,
    string GrammaticalCategory);
