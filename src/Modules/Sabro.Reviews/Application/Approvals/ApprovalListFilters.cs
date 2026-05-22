using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.Approvals;

public sealed record ApprovalListFilters(
    ApprovalTargetType? TargetType = null,
    ApprovalStatus? Status = null,
    Guid? SourceId = null,
    int? ChapterNumber = null,
    int? VerseNumber = null,
    int? Version = null,
    Guid? AnnotationId = null,
    string? DecisionByLogtoUserId = null);
