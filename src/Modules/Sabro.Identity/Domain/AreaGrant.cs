namespace Sabro.Identity.Domain;

/// <summary>
/// One person's access to one <see cref="ContentArea"/>.
/// </summary>
/// <remarks>
/// A child record of <see cref="UserProfile"/> keyed on (profile, area), so the
/// database enforces one permission per area per person. Replaces the single
/// <c>Role</c> column for area access, which forced area and level onto one
/// dimension and made "reviewer for Shmo, editor for the Lexicon" unrepresentable.
/// </remarks>
public sealed class AreaGrant
{
    private AreaGrant(ContentArea area, AreaAccess access)
    {
        Area = area;
        Access = access;
    }

    // EF Core.
    private AreaGrant()
    {
    }

    public ContentArea Area { get; private set; }

    public AreaAccess Access { get; private set; }

    internal static AreaGrant Create(ContentArea area, AreaAccess access) => new(area, access);

    internal void ChangeAccess(AreaAccess access) => Access = access;
}
