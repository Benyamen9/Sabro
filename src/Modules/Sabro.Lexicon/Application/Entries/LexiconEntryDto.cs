using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Application.Entries;

public sealed record LexiconEntryDto(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    Guid? RootId,
    string SblTransliteration,
    IReadOnlyList<string> TransliterationVariants,
    GrammaticalCategory GrammaticalCategory,
    string? Morphology,
    IReadOnlyList<LexiconMeaningDto> Meanings,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
