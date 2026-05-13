using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.Approvals;

public sealed record ApprovalDto(
    Guid Id,
    ApprovalTargetType TargetType,
    Guid SourceId,
    int ChapterNumber,
    int? VerseNumber,
    int? Version,
    Guid? AnnotationId,
    ApprovalStatus Status,
    string DecisionByLogtoUserId,
    DateTimeOffset DecisionAt,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
