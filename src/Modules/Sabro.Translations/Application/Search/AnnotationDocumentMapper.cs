using Sabro.Translations.Domain;

namespace Sabro.Translations.Application.Search;

internal static class AnnotationDocumentMapper
{
    public static AnnotationSearchDocument Map(Annotation annotation, Segment segment) => new()
    {
        Id = annotation.Id.ToString("D"),
        SegmentId = segment.Id.ToString("D"),
        SourceId = segment.SourceId.ToString("D"),
        ChapterNumber = segment.ChapterNumber,
        VerseNumber = segment.VerseNumber,
        AnchorStart = annotation.AnchorStart,
        AnchorEnd = annotation.AnchorEnd,
        Body = annotation.Body,
        Version = annotation.Version,
        CreatedAtUnix = annotation.CreatedAt.ToUnixTimeSeconds(),
    };
}
