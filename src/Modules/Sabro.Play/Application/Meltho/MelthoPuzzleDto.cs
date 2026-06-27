namespace Sabro.Play.Application.Meltho;

/// <summary>
/// Today's Meltho puzzle: the served date plus the target word's playable
/// projection. Meltho evaluates guesses client-side, so it receives the answer
/// word and length here; the meanings/vocalized/transliteration support the
/// post-game reveal without a second round trip.
/// </summary>
public sealed record MelthoPuzzleDto(
    DateOnly Date,
    Guid LexiconEntryId,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    string GrammaticalCategory,
    int PlayableLength,
    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings);
