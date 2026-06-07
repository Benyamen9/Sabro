namespace Sabro.Play.Application.Meltha;

/// <summary>
/// Today's Melthā puzzle: the served date plus the target word's playable
/// projection. Melthā evaluates guesses client-side, so it receives the answer
/// word and length here; the meanings/vocalized/transliteration support the
/// post-game reveal without a second round trip.
/// </summary>
public sealed record MelthaPuzzleDto(
    DateOnly Date,
    Guid LexiconEntryId,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    int PlayableLength,
    IReadOnlyList<MelthaPuzzleMeaningDto> Meanings);
