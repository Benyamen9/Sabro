using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Domain;

/// <summary>
/// One section of the Beth Gazo — the farde, the madroshe, the gnize and the rest.
/// </summary>
/// <remarks>
/// <para>
/// <b>A section decides which modes exist for its chants.</b> That is the whole
/// reason this type exists, and it is why the allowed modes live here rather than
/// as flags on <see cref="BethGazoMode"/>. Two rules from the owner, both of them
/// the same rule seen from different ends:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <b>The madroshe have no mode at all.</b> Here that is a section with an
///     empty <see cref="AllowedModeIds"/> — not a nullable flag, not a special
///     case in the game. A section that allows no mode simply cannot be asked
///     one.
///     </description>
///   </item>
///   <item>
///     <description>
///     <b>The <i>mshaḥelfotho</i> belongs to the farde only.</b> Here that is one
///     mode present in the farde's allowed set and absent from everyone else's.
///     </description>
///   </item>
/// </list>
/// <para>
/// Modelling both as one link is deliberate. The alternative — a
/// <c>HasModes</c> flag plus an <c>IncludesMshahelfotho</c> flag — would name a
/// specific mode in the schema, so the next section-specific rule would be another
/// column and another deploy. This way a rule about which modes a section admits is
/// a row an editor adds, exactly as the modes themselves are.
/// </para>
/// <para>
/// <b>The set of sections is open.</b> The treasury has more sections than the ones
/// seeded, and the owner adds them as he works through it — so nothing in the
/// domain, the API or the game may assume a count here either. This is the same
/// standing rule that made the modes a table rather than an enum.
/// </para>
/// </remarks>
public sealed class BethGazoSection : Entity<Guid>, IAggregateRoot
{
    public const int MaxNameLength = 64;

    private readonly List<BethGazoSectionMode> allowedModes = [];

    private BethGazoSection(string name, int position)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Name = name;
        Position = position;
    }

    private BethGazoSection()
    {
    }

    /// <summary>The section's name in SBL transliteration, e.g. "Farde", "Madroshe".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Where the section sits in the treasury's order. A sort key only, exactly as
    /// <see cref="BethGazoMode.Position"/> is — the section is identified by its id,
    /// so reordering never rewrites a chant's link.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// The modes a chant in this section may carry. <b>Empty means the section has
    /// no modes</b> — that is the madroshe, and it is a real answer rather than
    /// missing data.
    /// </summary>
    public IReadOnlyCollection<BethGazoSectionMode> AllowedModes => allowedModes;

    /// <summary>Convenience over <see cref="AllowedModes"/> for the invariant checks.</summary>
    public IReadOnlyCollection<Guid> AllowedModeIds => allowedModes.Select(m => m.ModeId).ToList();

    /// <summary>
    /// Whether a chant in this section is asked for a mode at all. False for the
    /// madroshe.
    /// </summary>
    public bool HasModes => allowedModes.Count > 0;

    public static Result<BethGazoSection> Create(string name, int position)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<BethGazoSection>.Failure(Error.Validation("Section name is required."));
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Result<BethGazoSection>.Failure(
                Error.Validation($"Section name must be at most {MaxNameLength} characters."));
        }

        if (position < 1)
        {
            return Result<BethGazoSection>.Failure(Error.Validation("Section position must be 1 or greater."));
        }

        return Result<BethGazoSection>.Success(new BethGazoSection(trimmed, position));
    }

    /// <summary>
    /// Moves the section to another slot in the treasury's order.
    /// </summary>
    /// <remarks>
    /// Takes the position rather than validating it, because a reorder is a swap and
    /// the intermediate step deliberately parks a section outside the valid range —
    /// <see cref="Position"/> is uniquely indexed, so one of the two has to step
    /// aside before the other can take its slot. The application layer owns that
    /// dance; the domain's job here is only to record where it ended up.
    /// </remarks>
    public void MoveTo(int position)
    {
        Position = position;
        Touch();
    }

    public Error? Rename(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Error.Validation("Section name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Error.Validation($"Section name must be at most {MaxNameLength} characters.");
        }

        Name = trimmed;
        Touch();
        return null;
    }

    /// <summary>
    /// Replaces the set of modes this section admits. Passing an empty set is
    /// meaningful, not a mistake — it is how a section is declared mode-less.
    /// </summary>
    /// <remarks>
    /// Emptying a section that already has chants carrying modes is refused by
    /// the application layer rather than here, because the domain cannot see
    /// those chants. See <c>ChantService</c>.
    /// </remarks>
    public Error? SetAllowedModes(IReadOnlyCollection<Guid> modeIds)
    {
        var distinct = (modeIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();

        if (distinct.Count != (modeIds?.Count ?? 0))
        {
            return Error.Validation("Allowed modes must be distinct, real mode ids.");
        }

        allowedModes.Clear();
        foreach (var modeId in distinct)
        {
            allowedModes.Add(new BethGazoSectionMode(Id, modeId));
        }

        Touch();
        return null;
    }

    /// <summary>
    /// Checks a chant's proposed mode against this section. The one place the two
    /// owner rules are actually enforced.
    /// </summary>
    public Error? ValidateMode(Guid? modeId)
    {
        if (!HasModes)
        {
            return modeId is null
                ? null
                : Error.Validation($"Chants in {Name} have no mode.");
        }

        if (modeId is null)
        {
            return Error.Validation($"A mode is required for chants in {Name}.");
        }

        return allowedModes.Any(m => m.ModeId == modeId.Value)
            ? null
            : Error.Validation($"That mode is not one of the modes used in {Name}.");
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
