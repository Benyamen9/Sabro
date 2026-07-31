namespace Sabro.Identity.Domain;

/// <summary>
/// Authorization role attached to a <see cref="UserProfile"/>. <see cref="Reader"/>
/// is the default for any authenticated user; <see cref="Owner"/> may do
/// everything. The area roles between them let a trusted collaborator work on one
/// body of content without being handed the rest.
/// </summary>
/// <remarks>
/// <para>
/// Stored as a string in the database, so adding a role is an ordinary code change
/// and a migration — never an enum-value reshuffle.
/// </para>
/// <para>
/// <b>One role per person.</b> Somebody cannot presently be a Lexicon editor and a
/// Shmo editor at once; that would need a child table rather than a column, and
/// nothing has asked for it yet. If it ever does, the migration is additive.
/// </para>
/// <para>
/// The distinction between an editor and a reviewer: an <c>Editor</c> changes
/// content directly, a <c>Reviewer</c> proposes changes the Owner accepts or
/// rejects. The proposal machinery does not exist yet — until it does, a reviewer
/// role grants read access to its area's backoffice and nothing more.
/// </para>
/// </remarks>
public enum Role
{
    /// <summary>Default for every authenticated user. No backoffice access.</summary>
    Reader,

    /// <summary>
    /// Invited reviewer for translation content, predating the area roles below and
    /// used by the (deferred) Reviews module. Not an area role.
    /// </summary>
    ExpertReviewer,

    /// <summary>May read the Lexicon backoffice and propose changes to it.</summary>
    LexiconReviewer,

    /// <summary>May create, edit, publish and delete Lexicon entries.</summary>
    LexiconEditor,

    /// <summary>May read the figures backoffice and propose changes to it.</summary>
    ShmoReviewer,

    /// <summary>May create, edit, publish and delete historical figures.</summary>
    ShmoEditor,

    /// <summary>Everything, including granting roles to other people.</summary>
    Owner,
}
