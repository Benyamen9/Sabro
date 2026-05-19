namespace Sabro.IntegrationTests;

/// <summary>
/// Comprehensive descriptor for a freshly-seeded Author + Source + TextVersion
/// + Segment chain. Returned by <see cref="TranslationsSeedExtensions.SeedSegmentAsync"/>.
/// Callers project to the fields they need — a superset is simpler than
/// per-call-site shapes that diverge.
/// </summary>
public sealed record SegmentSeed(
    Guid SegmentId,
    Guid SourceId,
    Guid AuthorId,
    Guid TextVersionId,
    int ChapterNumber,
    int VerseNumber);
