namespace Sabro.Translations.Application.Search;

/// <summary>
/// Public projection of an annotation search hit. Carries the denormalized
/// parent (Source, Chapter, Verse) coordinates so callers can render a
/// result row without a follow-up fetch against the relational store.
/// </summary>
public sealed record AnnotationSearchHitDto(
    Guid Id,
    Guid SegmentId,
    Guid SourceId,
    int ChapterNumber,
    int VerseNumber,
    int AnchorStart,
    int AnchorEnd,
    string Body,
    int Version);
