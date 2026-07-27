using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Historical.Domain;

/// <summary>
/// A guessable person in the Shmo roster: Biblical figures (Peshitta, both
/// Testaments) and Syriac patristic/ecclesiastical figures alike, in one shared
/// pool. Every field besides <see cref="Tradition"/> is required from creation;
/// tradition may be filled in later, so a figure can be drafted from a name and
/// finished before publication — the same incremental-authoring shape the Lexicon
/// uses for its glosses.
/// </summary>
public sealed class HistoricalFigure : Entity<Guid>, IAggregateRoot
{
    /// <summary>
    /// Widest centuries the roster accepts, guarding typos (a 4-digit year typed
    /// into a century field). Signed: negative is BC, positive is AD, and there is
    /// no century zero.
    /// </summary>
    private const int MinEra = -40;
    private const int MaxEra = 21;

    private HistoricalFigure(NormalizedFields fields)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Status = HistoricalFigureStatus.Draft;
        PlayableInShmo = false;
        Apply(fields);
    }

    private HistoricalFigure()
    {
    }

    /// <summary>Canonical display name, e.g. "Jacob of Edessa". The win condition of a Shmo round.</summary>
    public string Name { get; private set; } = string.Empty;

    public HistoricalFigureCategory Category { get; private set; }

    /// <summary>
    /// Signed century: <c>-10</c> is the 10th century BC, <c>7</c> the 7th century AD.
    /// Drives Shmo's higher/lower arrow hint, so it is a number rather than a label.
    /// </summary>
    public int Era { get; private set; }

    public HistoricalFigureRole Role { get; private set; }

    public HistoricalFigureRegion Region { get; private set; }

    /// <summary>
    /// Ecclesiastical tradition, or null while still being drafted. Publication
    /// requires a value — including the explicit <see cref="HistoricalFigureTradition.NotApplicable"/>,
    /// which is a real answer for pre-Christian figures, not a missing one.
    /// </summary>
    public HistoricalFigureTradition? Tradition { get; private set; }

    public HistoricalFigureGender Gender { get; private set; }

    public HistoricalFigureStatus Status { get; private set; }

    /// <summary>Editorial opt-in to the Shmo rotation. Only a published figure may carry it.</summary>
    public bool PlayableInShmo { get; private set; }

    public static Result<HistoricalFigure> Create(
        string name,
        HistoricalFigureCategory category,
        int era,
        HistoricalFigureRole role,
        HistoricalFigureRegion region,
        HistoricalFigureGender gender,
        HistoricalFigureTradition? tradition = null)
    {
        var normalized = Normalize(name, category, era, role, region, gender, tradition);
        if (!normalized.IsSuccess)
        {
            return Result<HistoricalFigure>.Failure(normalized.Error!);
        }

        return Result<HistoricalFigure>.Success(new HistoricalFigure(normalized.Value!));
    }

    /// <summary>
    /// Replaces the editable fields. Does not change <see cref="Status"/> or
    /// <see cref="PlayableInShmo"/>. A published figure must keep a tradition — an
    /// edit that would clear it is rejected; unpublish first.
    /// </summary>
    public Error? Update(
        string name,
        HistoricalFigureCategory category,
        int era,
        HistoricalFigureRole role,
        HistoricalFigureRegion region,
        HistoricalFigureGender gender,
        HistoricalFigureTradition? tradition = null)
    {
        var normalized = Normalize(name, category, era, role, region, gender, tradition);
        if (!normalized.IsSuccess)
        {
            return normalized.Error;
        }

        if (Status == HistoricalFigureStatus.Published && normalized.Value!.Tradition is null)
        {
            return Error.Validation(
                "A published figure must keep a tradition. Unpublish before removing it.");
        }

        Apply(normalized.Value!);
        Touch();
        return null;
    }

    /// <summary>Promotes a draft to published. Requires a tradition. Idempotent when already published.</summary>
    public Error? Publish()
    {
        if (Status == HistoricalFigureStatus.Published)
        {
            return null;
        }

        if (Tradition is null)
        {
            return Error.Validation("A tradition is required to publish a figure.");
        }

        Status = HistoricalFigureStatus.Published;
        Touch();
        return null;
    }

    /// <summary>Returns the figure to draft and clears the playable flag (a draft can never be playable).</summary>
    public void ReturnToDraft()
    {
        if (Status == HistoricalFigureStatus.Draft && !PlayableInShmo)
        {
            return;
        }

        Status = HistoricalFigureStatus.Draft;
        PlayableInShmo = false;
        Touch();
    }

    /// <summary>Sets the editorial playable flag. Marking playable requires the figure to be published.</summary>
    public Error? SetPlayable(bool playable)
    {
        if (playable && Status != HistoricalFigureStatus.Published)
        {
            return Error.Conflict("Only published figures can be marked playable.");
        }

        if (PlayableInShmo == playable)
        {
            return null;
        }

        PlayableInShmo = playable;
        Touch();
        return null;
    }

    private static Result<NormalizedFields> Normalize(
        string name,
        HistoricalFigureCategory category,
        int era,
        HistoricalFigureRole role,
        HistoricalFigureRegion region,
        HistoricalFigureGender gender,
        HistoricalFigureTradition? tradition)
    {
        var trimmedName = (name ?? string.Empty).Trim();
        if (trimmedName.Length == 0)
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Name is required."));
        }

        if (trimmedName.Length > 256)
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Name must be at most 256 characters."));
        }

        if (!Enum.IsDefined(category))
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Category is not a defined value."));
        }

        if (!Enum.IsDefined(role))
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Role is not a defined value."));
        }

        if (!Enum.IsDefined(region))
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Region is not a defined value."));
        }

        if (!Enum.IsDefined(gender))
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Gender is not a defined value."));
        }

        if (tradition.HasValue && !Enum.IsDefined(tradition.Value))
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Tradition is not a defined value."));
        }

        if (era == 0)
        {
            return Result<NormalizedFields>.Failure(Error.Validation("Era must not be zero — there is no century zero."));
        }

        if (era < MinEra || era > MaxEra)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation($"Era must be between {MinEra} and {MaxEra}."));
        }

        return Result<NormalizedFields>.Success(
            new NormalizedFields(trimmedName, category, era, role, region, gender, tradition));
    }

    private void Apply(NormalizedFields fields)
    {
        Name = fields.Name;
        Category = fields.Category;
        Era = fields.Era;
        Role = fields.Role;
        Region = fields.Region;
        Gender = fields.Gender;
        Tradition = fields.Tradition;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private readonly record struct NormalizedFields(
        string Name,
        HistoricalFigureCategory Category,
        int Era,
        HistoricalFigureRole Role,
        HistoricalFigureRegion Region,
        HistoricalFigureGender Gender,
        HistoricalFigureTradition? Tradition);
}
