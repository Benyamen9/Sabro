namespace Sabro.Identity.Domain;

/// <summary>
/// What each person may do, in one place.
/// </summary>
/// <remarks>
/// <para>
/// Kept as a single table rather than scattered <c>role != Role.Owner</c> checks
/// across controllers. Those checks are how an area silently ends up with
/// different rules from its neighbour, and they are invisible until someone is
/// wrongly let in or wrongly locked out.
/// </para>
/// <para>
/// This answers "may this person do X". It does not answer "is this person allowed
/// through the door at all" — that remains the <c>api:v1:admin</c> scope from
/// Logto, checked before any of this. Two questions, two answers: Logto decides
/// who is staff, Sabro decides which rooms they may enter.
/// </para>
/// <para>
/// <b>Takes a profile, not a role.</b> Access is now two independent facts — the
/// non-area <see cref="Role"/> and a per-area grant — because one person may
/// review Shmo while editing the Lexicon. A single role could not say that.
/// <see cref="Role.Owner"/> is the only role that still implies area access, and
/// this is the one place that says so.
/// </para>
/// </remarks>
public static class RolePermissions
{
    /// <summary>May create, edit, publish and delete the area's content.</summary>
    public static bool CanEdit(IAccessProfile profile, ContentArea area) =>
        IsOwner(profile) || profile?.AccessFor(area) == AreaAccess.Editor;

    /// <summary>
    /// May open the area's backoffice — editors plus reviewers, since a reviewer
    /// has to see the content to have an opinion about it.
    /// </summary>
    public static bool CanViewBackoffice(IAccessProfile profile, ContentArea area) =>
        CanEdit(profile, area) || profile?.AccessFor(area) == AreaAccess.Reviewer;

    /// <summary>
    /// May propose corrections to the area. Reviewers only: an editor changes the
    /// content directly, so a proposal from one would be a decision waiting on its
    /// own author — and the Owner is not a reviewer of their own work.
    /// </summary>
    public static bool CanPropose(IAccessProfile profile, ContentArea area) =>
        profile?.AccessFor(area) == AreaAccess.Reviewer;

    /// <summary>
    /// May propose corrections to translation content. The pre-existing translations
    /// reviewer, kept distinct from the area grants it predates.
    /// </summary>
    public static bool CanProposeTranslationEdit(IAccessProfile profile) =>
        profile?.Role == Role.ExpertReviewer;

    /// <summary>
    /// May accept or reject proposals. Owner-only, and deliberately not implied by
    /// any editor grant — an editor changes content, but deciding whose correction
    /// stands is the Owner's scholarly judgement.
    /// </summary>
    public static bool CanDecideProposals(IAccessProfile profile) => IsOwner(profile);

    /// <summary>
    /// May grant and revoke other people's access. Deliberately Owner-only and
    /// deliberately not implied by any editor grant: being trusted with content is
    /// not the same as being trusted with who else gets in.
    /// </summary>
    public static bool CanAssignRoles(IAccessProfile profile) => IsOwner(profile);

    /// <summary>
    /// May reach any backoffice area at all. Used to decide whether to show the
    /// backoffice entry point rather than to authorise a specific action.
    /// </summary>
    public static bool CanViewAnyBackoffice(IAccessProfile profile) =>
        IsOwner(profile) || Enum.GetValues<ContentArea>().Any(area => CanViewBackoffice(profile, area));

    private static bool IsOwner(IAccessProfile? profile) => profile?.Role == Role.Owner;
}
