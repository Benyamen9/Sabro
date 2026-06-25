using Sabro.Shared.Text;

namespace Sabro.Lexicon.Application.Entries;

/// <summary>
/// Full read-only projection of a lexicon entry for the Meltho library detail page: the info
/// table fields plus the computed per-letter <see cref="Composition"/>. The grammatical
/// category is a plain string so the Play module stays decoupled from the Lexicon enum.
/// </summary>
public sealed record LexiconLibraryDetail(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    IReadOnlyList<string> TransliterationVariants,
    string GrammaticalCategory,
    string? Morphology,
    int PlayableLength,
    string? Root,
    IReadOnlyList<LexiconMeaningDto> Meanings,
    IReadOnlyList<SyriacLetter> Composition);
