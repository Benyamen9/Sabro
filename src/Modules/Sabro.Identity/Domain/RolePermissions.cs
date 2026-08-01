namespace Sabro.Identity.Domain;

/// <summary>
/// What each <see cref="Role"/> may do, in one place.
/// </summary>
/// <remarks>
/// <para>
/// Kept as a single table rather than scattered <c>role != Role.Owner</c> checks
/// across controllers. Those checks are how an area silently ends up with
/// different rules from its neighbour, and they are invisible until someone is
/// wrongly let in or wrongly locked out.
/// </para>
/// <para>
/// This answers "may this role do X". It does not answer "is this person allowed
/// through the door at all" — that remains the <c>api:v1:admin</c> scope from
/// Logto, checked before any of this. Two questions, two answers: Logto decides
/// who is staff, Sabro decides which rooms they may enter.
/// </para>
/// </remarks>
public static class RolePermissions
{
    /// <summary>May create, edit, publish and delete Lexicon entries.</summary>
    public static bool CanEditLexicon(Role role) =>
        role is Role.Owner or Role.LexiconEditor;

    /// <summary>
    /// May open the Lexicon backoffice — editors plus reviewers, since a reviewer
    /// has to see the content to have an opinion about it.
    /// </summary>
    public static bool CanViewLexiconBackoffice(Role role) =>
        CanEditLexicon(role) || role is Role.LexiconReviewer;

    /// <summary>May create, edit, publish and delete historical figures.</summary>
    public static bool CanEditFigures(Role role) =>
        role is Role.Owner or Role.ShmoEditor;

    /// <summary>May open the figures backoffice — editors plus reviewers.</summary>
    public static bool CanViewFiguresBackoffice(Role role) =>
        CanEditFigures(role) || role is Role.ShmoReviewer;

    /// <summary>
    /// May propose corrections to Lexicon entries. Reviewers only: an editor changes
    /// the entry directly, so a proposal from one would be a decision waiting on its
    /// own author.
    /// </summary>
    public static bool CanProposeLexiconEdit(Role role) => role is Role.LexiconReviewer;

    /// <summary>May propose corrections to historical figures. Reviewers only.</summary>
    public static bool CanProposeFigureEdit(Role role) => role is Role.ShmoReviewer;

    /// <summary>
    /// May propose corrections to translation content. The pre-existing translations
    /// reviewer, kept distinct from the area roles it predates.
    /// </summary>
    public static bool CanProposeTranslationEdit(Role role) => role is Role.ExpertReviewer;

    /// <summary>
    /// May accept or reject proposals. Owner-only, and deliberately not implied by any
    /// editor role — an editor changes content, but deciding whose correction stands is
    /// the Owner's scholarly judgement.
    /// </summary>
    public static bool CanDecideProposals(Role role) => role is Role.Owner;

    /// <summary>
    /// May grant and revoke other people's roles. Deliberately Owner-only and
    /// deliberately not implied by any editor role: being trusted with content is
    /// not the same as being trusted with who else gets in.
    /// </summary>
    public static bool CanAssignRoles(Role role) => role is Role.Owner;

    /// <summary>
    /// May reach any backoffice area at all. Used to decide whether to show the
    /// backoffice entry point rather than to authorise a specific action.
    /// </summary>
    public static bool CanViewAnyBackoffice(Role role) =>
        CanViewLexiconBackoffice(role) || CanViewFiguresBackoffice(role);
}
