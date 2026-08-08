using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Domain;

/// <summary>
/// One mode of the Beth Gazo — Qadmoyo, Trayono, Tlithoyo and the rest.
/// </summary>
/// <remarks>
/// <para>
/// <b>A reference table, deliberately not a string-converted enum.</b> Every other
/// enum in this schema is an enum because its values change when the code changes.
/// These do not: the owner adds modes as he works through the tradition, and he
/// told us plainly that "some have more than eight". Eight is a floor, not a
/// ceiling — so the set has to be data an editor can extend, not a type a deploy
/// has to widen.
/// </para>
/// <para>
/// Nothing in the domain, the API or the game may therefore assume the count is
/// eight, or that <see cref="Position"/> stops at eight.
/// </para>
/// </remarks>
public sealed class BethGazoMode : Entity<Guid>, IAggregateRoot
{
    public const int MaxNameLength = 64;

    private BethGazoMode(string name, int position)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Name = name;
        Position = position;
    }

    private BethGazoMode()
    {
    }

    /// <summary>The mode's Syriac ordinal name in SBL transliteration, e.g. "Qadmoyo".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Where the mode sits in the traditional sequence — 1 for Qadmoyo, 2 for
    /// Trayono, and so on. A sort key, not an identity: the mode is identified by
    /// <see cref="Entity{TId}.Id"/>, so renumbering never rewrites a chant's link.
    /// </summary>
    public int Position { get; private set; }

    public static Result<BethGazoMode> Create(string name, int position)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<BethGazoMode>.Failure(Error.Validation("Mode name is required."));
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Result<BethGazoMode>.Failure(
                Error.Validation($"Mode name must be at most {MaxNameLength} characters."));
        }

        if (position < 1)
        {
            return Result<BethGazoMode>.Failure(Error.Validation("Mode position must be 1 or greater."));
        }

        return Result<BethGazoMode>.Success(new BethGazoMode(trimmed, position));
    }

    /// <summary>
    /// Moves the mode to another slot in the traditional order.
    /// </summary>
    /// <remarks>
    /// Takes the position rather than validating it, because a reorder is a swap and
    /// the intermediate step deliberately parks a mode outside the valid range —
    /// <see cref="Position"/> is uniquely indexed, so one of the two has to step aside
    /// before the other can take its slot. The application layer owns that dance.
    /// </remarks>
    public void MoveTo(int position)
    {
        Position = position;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Error? Rename(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Error.Validation("Mode name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Error.Validation($"Mode name must be at most {MaxNameLength} characters.");
        }

        Name = trimmed;
        UpdatedAt = DateTimeOffset.UtcNow;
        return null;
    }
}
