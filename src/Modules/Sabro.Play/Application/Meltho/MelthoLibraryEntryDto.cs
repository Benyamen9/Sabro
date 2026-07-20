namespace Sabro.Play.Application.Meltho;

/// <summary>
/// One row of the Meltho word library: a past word, its glosses, when it was last served, how
/// many past days it appeared on, and the enrichment the list renders and sorts by.
/// </summary>
public sealed record MelthoLibraryEntryDto(
    DateOnly LastPlayedOn,
    Guid LexiconEntryId,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    int PlayableLength,
    int TimesPlayed,
    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings);
