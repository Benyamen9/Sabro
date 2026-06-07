using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Application.Entries;

/// <summary>
/// Full replacement of an entry's editable fields. Does not carry status or the
/// playable flag — those move through the dedicated publish/unpublish/playable
/// operations. The target entry is identified by the route id, not this body.
/// </summary>
public sealed record UpdateLexiconEntryRequest(
    string SyriacUnvocalized,
    string? SblTransliteration,
    GrammaticalCategory GrammaticalCategory,
    string? SyriacVocalized = null,
    Guid? RootId = null,
    IReadOnlyList<string>? TransliterationVariants = null,
    string? Morphology = null,
    IReadOnlyList<CreateLexiconMeaningRequest>? Meanings = null);
