using Sabro.Shared.Text;

namespace Sabro.Play.Application.Meltho;

/// <summary>
/// The Meltho library detail for one past word: the info-table fields, the computed per-letter
/// <see cref="Composition"/>, and every past date the word was served.
/// </summary>
public sealed record MelthoLibraryDetailDto(
    Guid LexiconEntryId,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    IReadOnlyList<string> TransliterationVariants,
    string GrammaticalCategory,
    string? Morphology,
    int PlayableLength,
    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings,
    IReadOnlyList<SyriacLetter> Composition,
    IReadOnlyList<DateOnly> PlayedOn);
