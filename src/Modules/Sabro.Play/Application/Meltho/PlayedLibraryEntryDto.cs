namespace Sabro.Play.Application.Meltho;

/// <summary>
/// One row of the unified library's "played in Meltho" filter: a word that is both currently
/// Published and has been served as a daily puzzle at least once. Distinct from
/// <see cref="MelthoLibraryEntryDto"/> (the standalone `/play/meltho/library` archive, which does
/// not re-check publish state) — this DTO backs the new `/api/v1/library` endpoint only, so the
/// existing Meltho-app contract stays untouched.
/// </summary>
public sealed record PlayedLibraryEntryDto(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    string GrammaticalCategory,
    int PlayableLength,
    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings,
    DateOnly LastPlayedOn,
    int TimesPlayed) : ISortableLibraryWord;
