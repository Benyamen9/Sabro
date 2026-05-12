using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.SuggestedEdits;

public sealed record SuggestedEditDto(
    Guid Id,
    SuggestedEditTargetType TargetType,
    Guid TargetId,
    int TargetVersion,
    string ProposedContent,
    string? Rationale,
    string SubmittedByLogtoUserId,
    SuggestedEditStatus Status,
    string? DecisionByLogtoUserId,
    DateTimeOffset? DecisionAt,
    string? DecisionNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
