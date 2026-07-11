using Sabro.Lexicon.Application.Entries;

namespace Sabro.Lexicon.Application.Dictionary;

/// <summary>
/// Public dictionary list projection: one published word with its glosses.
/// Deliberately carries no editorial state (<c>Status</c>) and no puzzle-pool
/// markers (<c>PlayableInMeltho</c>) — the dictionary is served anonymously and
/// must not let clients enumerate future Meltho words. The grammatical category
/// is a plain string so the wire contract matches the library DTOs.
/// </summary>
public sealed record DictionaryEntryListItem(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    string GrammaticalCategory,
    int LetterCount,
    IReadOnlyList<LexiconMeaningDto> Meanings);
