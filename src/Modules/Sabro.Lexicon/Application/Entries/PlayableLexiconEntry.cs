namespace Sabro.Lexicon.Application.Entries;

/// <summary>Flat, read-only projection of a lexicon entry as needed to play and reveal a Melthā puzzle.</summary>
public sealed record PlayableLexiconEntry(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    int PlayableLength,
    IReadOnlyList<LexiconMeaningDto> Meanings);
