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
        int targetVersion,
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
        ProposedContent = proposedContent;
        SubmittedByLogtoUserId = submittedByLogtoUserId;
        Rationale = rationale;
        Status = SuggestedEditStatus.Pending;
    }

    public SuggestedEditTargetType TargetType { get; private set; }

    public Guid TargetId { get; private set; }

    /// <summary>
    /// Version of the target content the suggestion was made against.
    /// Lets the Owner detect that the target has moved on since the
    /// suggestion was filed (frontend can surface "this suggestion is
    /// against version N, current is N+1").
    /// </summary>
    public int TargetVersion { get; private set; }

    public string ProposedContent { get; private set; }

    public string? Rationale { get; private set; }

    public string SubmittedByLogtoUserId { get; private set; }

    public SuggestedEditStatus Status { get; private set; }

    public string? DecisionByLogtoUserId { get; private set; }

    public DateTimeOffset? DecisionAt { get; private set; }

    public string? DecisionNote { get; private set; }

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

        if (targetId == Guid.Empty)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("TargetId is required."));
        }

        if (targetVersion < 1)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("TargetVersion must be 1 or greater."));
        }

        var trimmedContent = (proposedContent ?? string.Empty).Trim();
        if (trimmedContent.Length == 0)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("ProposedContent is required."));
        }

        var trimmedSubmittedBy = (submittedByLogtoUserId ?? string.Empty).Trim();
        if (trimmedSubmittedBy.Length == 0)
        {
            return Result<SuggestedEdit>.Failure(Error.Validation("SubmittedByLogtoUserId is required."));
        }

        var normalizedRationale = string.IsNullOrWhiteSpace(rationale) ? null : rationale.Trim();

        return Result<SuggestedEdit>.Success(new SuggestedEdit(
            targetType,
            targetId,
            targetVersion,
            trimmedContent,
            trimmedSubmittedBy,
            normalizedRationale));
    }

    public Error? Accept(string decidedByLogtoUserId, string? note = null) =>
        ApplyDecision(SuggestedEditStatus.Accepted, decidedByLogtoUserId, note);

    public Error? Reject(string decidedByLogtoUserId, string? note = null) =>
        ApplyDecision(SuggestedEditStatus.Rejected, decidedByLogtoUserId, note);

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
