namespace Sabro.Translations.Application.Search;

/// <summary>
/// Public projection of a Segment search hit. Reflects the latest indexed
/// version of each segment — older versions remain in Postgres for audit
/// but are not returned by search.
/// </summary>
public sealed record SegmentSearchHitDto(
    Guid Id,
    Guid SourceId,
    int ChapterNumber,
    int VerseNumber,
    Guid TextVersionId,
    string Content,
    int Version);
