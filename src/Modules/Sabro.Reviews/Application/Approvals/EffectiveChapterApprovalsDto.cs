namespace Sabro.Reviews.Application.Approvals;

/// <summary>
/// Resolved approval state for a single chapter. <see cref="ChapterApproval"/>
/// is the latest chapter-level row (or null if none) and provides the cascade
/// default. <see cref="VerseApprovals"/> contains the latest verse-level row
/// per <c>VerseNumber</c> — these override the chapter verdict on a per-verse
/// basis. <see cref="AnnotationApprovals"/> contains the latest annotation
/// row per <c>AnnotationId</c> under the chapter and is standalone — annotation
/// approvals do not cascade from or to chapter/verse rows. Callers compose
/// effective per-verse status as <c>VerseApprovals[verse] ?? ChapterApproval</c>
/// and per-annotation status as <c>AnnotationApprovals[annotationId]</c> only.
/// </summary>
public sealed record EffectiveChapterApprovalsDto(
    Guid SourceId,
    int ChapterNumber,
    ApprovalDto? ChapterApproval,
    IReadOnlyList<ApprovalDto> VerseApprovals,
    IReadOnlyList<ApprovalDto> AnnotationApprovals);
