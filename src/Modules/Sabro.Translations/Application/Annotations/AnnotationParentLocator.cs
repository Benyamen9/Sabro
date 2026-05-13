namespace Sabro.Translations.Application.Annotations;

/// <summary>
/// Cross-module lookup result describing where an <c>Annotation</c> lives in
/// the canonical (Source, Chapter, Verse) hierarchy. Consumed by Reviews to
/// denormalize the parent locator onto an annotation-targeted Approval row.
/// </summary>
public sealed record AnnotationParentLocator(
    Guid AnnotationId,
    int AnnotationVersion,
    Guid SegmentId,
    Guid SourceId,
    int ChapterNumber,
    int VerseNumber);
