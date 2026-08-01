namespace Sabro.Identity.Domain;

/// <summary>
/// A body of editorial content access is granted over.
/// </summary>
/// <remarks>
/// <para>
/// Only content that somebody curates appears here. Mno has no entry because its
/// daily equation is generated rather than edited — there is nothing to grant
/// access to. Adding an area (Nahlo, Stories) is a value here, a case in
/// <c>RolePermissions</c>, and a row in the backoffice grid.
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
}
