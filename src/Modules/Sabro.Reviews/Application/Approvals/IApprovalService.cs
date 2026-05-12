using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Application.Approvals;

public interface IApprovalService
{
    /// <summary>
    /// Records the Owner's verdict on a Segment version or a Chapter. Each call
    /// appends a new row — re-deciding does not mutate prior rows. Caller must
    /// be <c>Owner</c>.
    /// </summary>
    Task<Result<ApprovalDto>> CreateAsync(
        CreateApprovalRequest request,
        string decidedByLogtoUserId,
        CancellationToken cancellationToken);

    Task<Result<ApprovalDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<ApprovalDto>>> ListAsync(
        ApprovalListFilters filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the latest chapter-level row plus the latest verse-level row per
    /// verse number for a given chapter. Per-verse effective status is computed
    /// by the caller as <c>VerseApprovals[verse] ?? ChapterApproval</c>.
    /// </summary>
    Task<Result<EffectiveChapterApprovalsDto>> GetEffectiveForChapterAsync(
        Guid sourceId,
        int chapterNumber,
        CancellationToken cancellationToken);
}
