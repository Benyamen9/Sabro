namespace Sabro.Reviews.Domain;

/// <summary>
/// What a <see cref="SuggestedEdit"/> targets. Chapter-level approval is handled
/// by a separate approval aggregate (deferred).
/// </summary>
/// <remarks>
/// <para>
/// Two shapes of proposal share this aggregate. <see cref="Segment"/> and
/// <see cref="Annotation"/> are <b>prose</b> targets from the (deferred)
/// Translations module: the whole content is replaced and the target carries a
/// version number. <see cref="LexiconEntry"/> and <see cref="HistoricalFigure"/>
/// are <b>field</b> targets: a proposal names one field, and staleness is judged
/// by the target's last-modified timestamp because those modules have no
/// versioning.
/// </para>
/// <para>
/// One aggregate rather than two, deliberately: a second proposal system would
/// drift from this one exactly the way <c>UserProfile.Role</c> and the Logto scope
/// drifted before they were reconciled.
/// </para>
/// </remarks>
public enum SuggestedEditTargetType
{
    Segment,
    Annotation,
    LexiconEntry,
    HistoricalFigure,
}
