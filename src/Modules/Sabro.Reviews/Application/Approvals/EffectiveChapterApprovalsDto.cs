namespace Sabro.Reviews.Application.Approvals;

/// <summary>
/// Resolved approval state for a single chapter. <see cref="ChapterApproval"/>
/// is the latest chapter-level row (or null if none) and provides the cascade
/// default. <see cref="VerseApprovals"/> contains the latest verse-level row
/// per <c>VerseNumber</c> — these override the chapter verdict on a per-verse
/// basis. Callers compose effective per-verse status as
/// <c>VerseApprovals[verse] ?? ChapterApproval</c>.
/// </summary>
public sealed record EffectiveChapterApprovalsDto(
    Guid SourceId,
    int ChapterNumber,
    ApprovalDto? ChapterApproval,
    IReadOnlyList<ApprovalDto> VerseApprovals);
