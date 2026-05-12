namespace Sabro.Reviews.Domain;

/// <summary>
/// What an <see cref="Approval"/> targets. Verse-level approval pins to a
/// specific <see cref="Sabro.Translations.Domain.Segment"/> version; chapter-
/// level approval covers every verse in the chapter via the lazy cascade
/// computed at read time. Annotation-level is deferred to a follow-up slice.
/// </summary>
public enum ApprovalTargetType
{
    Segment,
    Chapter,
}
