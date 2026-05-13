namespace Sabro.Reviews.Domain;

/// <summary>
/// What an <see cref="Approval"/> targets. Verse-level approval pins to a
/// specific <see cref="Sabro.Translations.Domain.Segment"/> version; chapter-
/// level approval covers every verse in the chapter via the lazy cascade
/// computed at read time. Annotation-level approval is standalone (no cascade
/// to or from chapter/verse rows) but denormalizes the parent locator on the
/// row so it shows up in chapter-scoped list queries.
/// </summary>
public enum ApprovalTargetType
{
    Segment,
    Chapter,
    Annotation,
}
