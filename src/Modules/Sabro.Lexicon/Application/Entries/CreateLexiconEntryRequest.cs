using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Application.Entries;

public sealed record CreateLexiconEntryRequest(
    string SyriacUnvocalized,
    string SblTransliteration,
    GrammaticalCategory GrammaticalCategory,
    string? SyriacVocalized = null,
    Guid? RootId = null,
    IReadOnlyList<string>? TransliterationVariants = null,
    string? Morphology = null,
    IReadOnlyList<CreateLexiconMeaningRequest>? Meanings = null);
