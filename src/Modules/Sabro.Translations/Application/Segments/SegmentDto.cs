namespace Sabro.Translations.Application.Segments;

public sealed record SegmentDto(
    Guid Id,
    Guid SourceId,
    int ChapterNumber,
    int VerseNumber,
    Guid TextVersionId,
    string Content,
    int Version,
    Guid? PreviousVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
