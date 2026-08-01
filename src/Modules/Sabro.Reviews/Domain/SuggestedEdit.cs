using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Domain;

/// <summary>
/// A proposed correction to an existing translation segment or annotation.
/// Reviews are decoupled from Translations: accepting a suggestion only
/// records the Owner's decision — it does not modify the target content.
/// The Owner separately edits the target via the Translations module, which
/// matches the business rule "suggestions never modify content directly".
/// </summary>
public sealed class SuggestedEdit : Entity<Guid>, IAggregateRoot
{
    private SuggestedEdit(
        SuggestedEditTargetType targetType,
        Guid targetId,
        int? targetVersion,
        DateTimeOffset? targetUpdatedAt,
        string? field,
        string? originalValue,
        string proposedContent,
        string submittedByLogtoUserId,
        string? rationale)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        TargetType = targetType;
        TargetId = targetId;
        TargetVersion = targetVersion;
        TargetUpdatedAt = targetUpdatedAt;
        Field = field;
        OriginalValue = originalValue;
        ProposedContent = proposedContent;
        SubmittedByLogtoUserId = submittedByLogtoUserId;
        Rationale = rationale;
        Status = SuggestedEditStatus.Pending;
    }

    public SuggestedEditTargetType TargetType { get; private set; }

    public Guid TargetId { get; private set; }

    /// <summary>
    /// Version of the target content the suggestion was made against, for prose
    /// targets. Lets the Owner detect that the target has moved on since the
    /// suggestion was filed (frontend can surface "this suggestion is
    /// against version N, current is N+1").
    /// <see langword="null"/> for field proposals — the Lexicon and Historical
    /// modules do not version their entities, so those use
    /// <see cref="TargetUpdatedAt"/> for the same purpose.
    /// </summary>
    public int? TargetVersion { get; private set; }

    /// <summary>
    /// The target's <c>UpdatedAt</c> as it stood when the proposal was filed, for
    /// field proposals. Comparing it against the target's current value is what
    /// reveals that the entry changed while the proposal sat in the queue — so the
    /// Owner is never shown a stale "before" without warning.
    /// </summary>
    /// <remarks>
    /// Read server-side from the owning module at submission time, never accepted
    /// from the client: a caller who could set it could hide the fact that they are
    /// proposing against content that has since moved.
    /// </remarks>
    public DateTimeOffset? TargetUpdatedAt { get; private set; }

    /// <summary>
    /// Which single field of the target this proposes a new value for, in the API's
    /// camelCase spelling (e.g. <c>syriacVocalized</c>, <c>meaning.fr</c>).
    /// <see langword="null"/> for prose targets, where the whole content is replaced.
    /// </summary>
    /// <remarks>
    /// One field per proposal, deliberately. Review happens field by field — "the
    /// French gloss is wrong" — and this lets the Owner accept that correction while
    /// rejecting a different one filed against the same entry.
    /// </remarks>
    public string? Field { get; private set; }

    /// <summary>
    /// What <see cref="Field"/> held at the moment the proposal was filed — the
    /// "before" half of the diff the Owner reviews.
    /// </summary>
    /// <remarks>
    /// Comparing this against the field's current value is what detects staleness,
    /// per field rather than per entity: editing an entry's English gloss must not
    /// mark a pending French-gloss proposal stale, or the warning becomes noise and
    /// stops being read. <see langword="null"/> is a real value here — it means the
    /// field was empty, which is the common case for a proposal that fills a gap.
    /// </remarks>
    public string? OriginalValue { get; private set; }

    /// <summary>
    /// Set when the Owner accepted this even though the field had changed since it
    /// was proposed. Recorded rather than merely allowed, so "we knowingly took an
    /// older correction over a newer edit" stays visible afterwards.
    /// </summary>
    public bool AcceptedDespiteChange { get; private set; }

    public string ProposedContent { get; private set; }

    public string? Rationale { get; private set; }

    public string SubmittedByLogtoUserId { get; private set; }

    public SuggestedEditStatus Status { get; private set; }

    public string? DecisionByLogtoUserId { get; private set; }

    public DateTimeOffset? DecisionAt { get; private set; }

    public string? DecisionNote { get; private set; }

    /// <summary>
    /// Proposes replacing the whole content of a prose target (a translation segment
    /// or an annotation), tracked against the target's version number.
    /// </summary>
    public static Result<SuggestedEdit> Create(
        SuggestedEditTargetType targetType,
        Guid targetId,
        int targetVersion,
        string proposedContent,
        string submittedByLogtoUserId,
        string? rationale = null)
    {
        if (!Enum.IsDefined(targetType))
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("TargetType is invalid."));
        }

        if (!IsProseTarget(targetType))
        {
            return Result<SuggestedEdit>.Failure(Error.Validation(
                $"{targetType} is a field target — use ProposeFieldChange."));
        }

        if (targetId == Guid.Empty)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("TargetId is required."));
        }

        if (targetVersion < 1)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("TargetVersion must be 1 or greater."));
        }

        var contentResult = NormalizeContent(proposedContent);
        if (!contentResult.IsSuccess)
        {
            return Result<SuggestedEdit>.Failure(contentResult.Error!);
        }

        var submittedByResult = NormalizeSubmittedBy(submittedByLogtoUserId);
        if (!submittedByResult.IsSuccess)
        {
            return Result<SuggestedEdit>.Failure(submittedByResult.Error!);
        }

        return Result<SuggestedEdit>.Success(new SuggestedEdit(
            targetType,
            targetId,
            targetVersion,
            targetUpdatedAt: null,
            field: null,
            originalValue: null,
            contentResult.Value!,
            submittedByResult.Value!,
            NormalizeRationale(rationale)));
    }

    /// <summary>
    /// Proposes a new value for one named field of a Lexicon entry or a historical
    /// figure, tracked against the target's last-modified timestamp.
    /// </summary>
    /// <remarks>
    /// <paramref name="field"/> is validated against the owning module's proposable
    /// list before this is called; the domain only enforces that a field target
    /// carries one. <paramref name="targetUpdatedAt"/> comes from the owning module,
    /// never from the caller.
    /// </remarks>
    public static Result<SuggestedEdit> ProposeFieldChange(
        SuggestedEditTargetType targetType,
        Guid targetId,
        string field,
        string proposedValue,
        string? originalValue,
        DateTimeOffset targetUpdatedAt,
        string submittedByLogtoUserId,
        string? rationale = null)
    {
        if (IsProseTarget(targetType) || !Enum.IsDefined(targetType))
        {
            return Result<SuggestedEdit>.Failure(Error.Validation(
                $"{targetType} is not a field target."));
        }

        if (targetId == Guid.Empty)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("TargetId is required."));
        }

        var trimmedField = (field ?? string.Empty).Trim();
        if (trimmedField.Length == 0)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("Field is required."));
        }

        var contentResult = NormalizeContent(proposedValue);
        if (!contentResult.IsSuccess)
        {
            return Result<SuggestedEdit>.Failure(contentResult.Error!);
        }

        var submittedByResult = NormalizeSubmittedBy(submittedByLogtoUserId);
        if (!submittedByResult.IsSuccess)
        {
            return Result<SuggestedEdit>.Failure(submittedByResult.Error!);
        }

        return Result<SuggestedEdit>.Success(new SuggestedEdit(
            targetType,
            targetId,
            targetVersion: null,
            targetUpdatedAt,
            trimmedField,
            originalValue,
            contentResult.Value!,
            submittedByResult.Value!,
            NormalizeRationale(rationale)));
    }

    /// <summary>
    /// True for targets whose whole content is replaced and which carry a version
    /// number, rather than being proposed against field by field.
    /// </summary>
    public static bool IsProseTarget(SuggestedEditTargetType targetType) =>
        targetType is SuggestedEditTargetType.Segment or SuggestedEditTargetType.Annotation;

    /// <summary>
    /// Records the Owner's acceptance. Set <paramref name="despiteChange"/> when the
    /// field has moved since the proposal was filed and the Owner has decided to take
    /// it anyway; the service refuses that case unless it was asked for explicitly.
    /// </summary>
    public Error? Accept(string decidedByLogtoUserId, string? note = null, bool despiteChange = false)
    {
        var error = ApplyDecision(SuggestedEditStatus.Accepted, decidedByLogtoUserId, note);
        if (error is null && despiteChange)
        {
            AcceptedDespiteChange = true;
        }

        return error;
    }

    public Error? Reject(string decidedByLogtoUserId, string? note = null) =>
        ApplyDecision(SuggestedEditStatus.Rejected, decidedByLogtoUserId, note);

    private static Result<string> NormalizeContent(string proposedContent)
    {
        var trimmed = (proposedContent ?? string.Empty).Trim();
        return trimmed.Length == 0
            ? Result<string>.Failure(Error.Validation("ProposedContent is required."))
            : Result<string>.Success(trimmed);
    }

    private static Result<string> NormalizeSubmittedBy(string submittedByLogtoUserId)
    {
        var trimmed = (submittedByLogtoUserId ?? string.Empty).Trim();
        return trimmed.Length == 0
            ? Result<string>.Failure(Error.Validation("SubmittedByLogtoUserId is required."))
            : Result<string>.Success(trimmed);
    }

    private static string? NormalizeRationale(string? rationale) =>
        string.IsNullOrWhiteSpace(rationale) ? null : rationale.Trim();

    private Error? ApplyDecision(SuggestedEditStatus newStatus, string decidedByLogtoUserId, string? note)
    {
        if (Status != SuggestedEditStatus.Pending)
        {
            return Error.Conflict($"SuggestedEdit is already {Status}.");
        }

        var trimmedDecidedBy = (decidedByLogtoUserId ?? string.Empty).Trim();
        if (trimmedDecidedBy.Length == 0)
        {
            return Error.Validation("DecisionByLogtoUserId is required.");
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var now = DateTimeOffset.UtcNow;

        Status = newStatus;
        DecisionByLogtoUserId = trimmedDecidedBy;
        DecisionAt = now;
        DecisionNote = normalizedNote;
        UpdatedAt = now;
        return null;
    }
}
