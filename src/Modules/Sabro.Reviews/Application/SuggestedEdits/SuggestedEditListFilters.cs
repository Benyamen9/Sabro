using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.SuggestedEdits;

public sealed record SuggestedEditListFilters(
    SuggestedEditStatus? Status = null,
    SuggestedEditTargetType? TargetType = null,
    Guid? TargetId = null,
    string? SubmittedByLogtoUserId = null);
