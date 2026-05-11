namespace Sabro.Lexicon.Application.Search;

/// <summary>
/// Public projection of a Lexicon search hit. Mirrors the indexed document's
/// shape (denormalized root form, flat meaning texts) so that search results
/// are self-contained — clients can render a result row without a follow-up
/// fetch against the relational store.
/// </summary>
public sealed record LexiconSearchHitDto(
    Guid Id,
    string SyriacUnvocalized,
    string? SyriacVocalized,
    string SblTransliteration,
    IReadOnlyList<string> TransliterationVariants,
    Guid? RootId,
    string? RootForm,
    string GrammaticalCategory,
    string? Morphology,
    IReadOnlyList<string> MeaningTexts,
    IReadOnlyList<string> MeaningLanguages);
