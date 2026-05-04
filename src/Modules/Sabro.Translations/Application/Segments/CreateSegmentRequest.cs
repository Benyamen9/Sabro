namespace Sabro.Translations.Application.Segments;

public sealed record CreateSegmentRequest(
    Guid SourceId,
    int ChapterNumber,
    int VerseNumber,
    Guid TextVersionId,
    string Content);
