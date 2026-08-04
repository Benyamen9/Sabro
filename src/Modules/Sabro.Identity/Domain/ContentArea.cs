namespace Sabro.Identity.Domain;

/// <summary>
/// A body of editorial content access is granted over.
/// </summary>
/// <remarks>
/// <para>
/// Only content that somebody curates appears here. Mno has no entry because its
/// daily equation is generated rather than edited — there is nothing to grant
/// access to. Adding an area (Stories, and whatever follows) is a value here and
/// a row in the backoffice grid. <c>RolePermissions</c> needs no change: it is
/// written against <c>Enum.GetValues&lt;ContentArea&gt;()</c>, so a new value is
/// grantable the moment it exists.
/// </para>
/// <para>
/// Stored as a string, like every other enum in the schema, so adding a value is
/// an ordinary migration rather than an enum-value reshuffle.
/// </para>
/// </remarks>
public enum ContentArea
{
    /// <summary>The Syriac lexicon, which also feeds Meltho's word pool.</summary>
    Lexicon,

    /// <summary>Shmo's roster of historical figures.</summary>
    Shmo,

    /// <summary>The Beth Gazo — the chants and modes Nahlo draws its daily puzzle from.</summary>
    Nahlo,
}
