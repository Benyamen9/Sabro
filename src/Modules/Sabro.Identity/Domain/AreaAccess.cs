namespace Sabro.Identity.Domain;

/// <summary>
/// How much somebody may do within one <see cref="ContentArea"/>.
/// </summary>
/// <remarks>
/// There is no <c>None</c> member on purpose: "no access" is the absence of a
/// permission row, not a value stored in one. A <c>None</c> would give two ways to
/// say the same thing, and the two would eventually disagree — a row saying None
/// and a missing row reading differently somewhere.
/// </remarks>
public enum AreaAccess
{
    /// <summary>May read the area's backoffice and propose corrections to it.</summary>
    Reviewer,

    /// <summary>May create, edit, publish and delete the area's content directly.</summary>
    Editor,
}
