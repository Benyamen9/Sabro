using Sabro.Lexicon.Application.Entries;
using Sabro.Shared.Text;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Dictionary detail: the library-shaped word projection plus one cross-module
/// fact — whether the word has appeared as a Meltho daily puzzle on a past day.
/// Today's puzzle deliberately reports <c>false</c> until tomorrow so the
/// dictionary can never be used to single out the live word.
/// </summary>
public sealed record DictionaryEntryDetailResponse(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    IReadOnlyList<string> TransliterationVariants,
    string GrammaticalCategory,
    string? Morphology,
    int LetterCount,
    string? Root,
    IReadOnlyList<LexiconMeaningDto> Meanings,
    IReadOnlyList<SyriacLetter> Composition,
    bool PlayedInMeltho);
