namespace Sabro.Play.Application.Meltho;

/// <summary>One row of the Meltho word library: a past word, its glosses, and when it was last served.</summary>
public sealed record MelthoLibraryEntryDto(
    DateOnly LastPlayedOn,
    Guid LexiconEntryId,
    string SyriacUnvocalized,
    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings);
