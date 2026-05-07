using Sabro.Translations.Domain;

namespace Sabro.Translations.Application.Search;

internal static class SegmentDocumentMapper
{
    public static SegmentSearchDocument Map(Segment segment) => new()
    {
        Id = segment.Id.ToString("D"),
        SourceId = segment.SourceId.ToString("D"),
        ChapterNumber = segment.ChapterNumber,
        VerseNumber = segment.VerseNumber,
        TextVersionId = segment.TextVersionId.ToString("D"),
        Content = segment.Content,
        Version = segment.Version,
        CreatedAtUnix = segment.CreatedAt.ToUnixTimeSeconds(),
    };
}
