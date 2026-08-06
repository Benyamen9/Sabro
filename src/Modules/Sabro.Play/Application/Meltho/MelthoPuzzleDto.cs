namespace Sabro.Play.Application.Meltho;

/// <summary>
/// Today's Meltho puzzle: the served date plus the target word's playable
/// projection. Meltho evaluates guesses client-side, so it receives the answer
/// word and length here; the meanings/vocalized/transliteration support the
/// post-game reveal without a second round trip.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DayNumber"/> is the 1-based count of calendar days since Meltho's
/// first puzzle — the "Meltho #51" a player reads. It belongs here rather than in
/// the client because it must be identical for everyone, and only Sabro knows
/// when the first puzzle was served.
/// </para>
/// <para>
/// Meltho derived it as "distinct past words + 1", which was only ever true while
/// no word repeated. Once the pool was exhausted the counter fell behind the real
/// day (42 against an actual 51), labelled repeats as new words, and — because a
/// repeat adds no distinct word — printed the same number on consecutive days.
/// </para>
/// </remarks>
public sealed record MelthoPuzzleDto(
    DateOnly Date,
    int DayNumber,
    Guid LexiconEntryId,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    string GrammaticalCategory,
    int PlayableLength,
    IReadOnlyList<MelthoPuzzleMeaningDto> Meanings);
