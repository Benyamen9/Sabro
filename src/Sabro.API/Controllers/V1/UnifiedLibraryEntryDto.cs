using Sabro.Lexicon.Application.Entries;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// One row of the unified `/api/v1/library` list: a dictionary word, optionally enriched with
/// Meltho play stats. <see cref="LastPlayedOn"/>/<see cref="TimesPlayed"/> are null when the word
/// has never been served (always true for every row when <c>playedInMeltho=false</c> filters to
/// none; when <c>playedInMeltho=true</c> every row has them, since that's the whole point of the
/// filter).
/// </summary>
public sealed record UnifiedLibraryEntryDto(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string? SblTransliteration,
    string GrammaticalCategory,
    int LetterCount,
    IReadOnlyList<LexiconMeaningDto> Meanings,
    DateOnly? LastPlayedOn,
    int? TimesPlayed);
