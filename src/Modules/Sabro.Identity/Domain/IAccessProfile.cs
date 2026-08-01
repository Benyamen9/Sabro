namespace Sabro.Identity.Domain;

/// <summary>
/// The two facts <see cref="RolePermissions"/> needs about somebody: their
/// non-area role, and their access to a given area.
/// </summary>
/// <remarks>
/// Exists so one permission table can serve both layers. The domain entity has
/// this shape, and so does the DTO the application layer passes around — without
/// it, either <c>RolePermissions</c> would need a second copy for DTOs (two tables
/// that drift), or the application layer would have to load aggregates purely to
/// ask a permission question.
/// </remarks>
public interface IAccessProfile
{
    Role Role { get; }

    /// <summary>Access to <paramref name="area"/>, or <see langword="null"/> for none.</summary>
    AreaAccess? AccessFor(ContentArea area);
}
