using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.SuggestedEdits;

public sealed record CreateSuggestedEditRequest(
    SuggestedEditTargetType TargetType,
    Guid TargetId,
    int TargetVersion,
    string ProposedContent,
    string? Rationale = null);
