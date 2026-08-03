using Sabro.Reviews.Domain;
using Sabro.Shared.Abstractions;

namespace Sabro.Reviews.Application.SuggestedEdits;

/// <summary>
/// A proposal as returned by the API. <c>TargetVersion</c> is set for prose targets
/// and <c>Field</c> + <c>TargetUpdatedAt</c> for field targets; exactly one of the
/// two pairs is populated, decided by <c>TargetType</c>.
/// </summary>
public sealed record SuggestedEditDto(
    Guid Id,
    SuggestedEditTargetType TargetType,
    Guid TargetId,
    int? TargetVersion,
    DateTimeOffset? TargetUpdatedAt,
    string? Field,
    string? OriginalValue,
    bool AcceptedDespiteChange,
    string ProposedContent,
    string? Rationale,
    string SubmittedByLogtoUserId,
    SuggestedEditStatus Status,
    string? DecisionByLogtoUserId,
    DateTimeOffset? DecisionAt,
    string? DecisionNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ProposalTargetLabel? TargetLabel = null);
