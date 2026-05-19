namespace Sabro.IntegrationTests;

/// <summary>
/// Comprehensive descriptor for a freshly-seeded Annotation rooted at a
/// specific (Source, Chapter, Verse). Returned by
/// <see cref="TranslationsSeedExtensions.SeedAnnotationAsync"/>. Includes the
/// parent Segment / Source / Author / TextVersion ids so call sites can
/// project to whatever subset they need.
/// </summary>
public sealed record AnnotationSeed(
    Guid AnnotationId,
    int AnnotationVersion,
    Guid SegmentId,
    Guid SourceId,
    Guid AuthorId,
    Guid TextVersionId,
    int ChapterNumber,
    int VerseNumber);
