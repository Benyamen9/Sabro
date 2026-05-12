namespace Sabro.Reviews.Domain;

/// <summary>
/// What a <see cref="SuggestedEdit"/> targets. Verse-level review is modelled
/// as a suggestion against a <see cref="Segment"/>; annotation-level review
/// targets an <see cref="Annotation"/>. Chapter-level approval is handled by
/// a separate approval aggregate (deferred).
/// </summary>
public enum SuggestedEditTargetType
{
    Segment,
    Annotation,
}
