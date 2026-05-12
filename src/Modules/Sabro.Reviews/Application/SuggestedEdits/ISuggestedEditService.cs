using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Application.SuggestedEdits;

public interface ISuggestedEditService
{
    /// <summary>
    /// Files a new proposed edit. Caller must be an <c>ExpertReviewer</c> —
    /// Readers cannot propose and the Owner edits directly via Translations
    /// rather than going through the review queue.
    /// </summary>
    Task<Result<SuggestedEditDto>> ProposeAsync(
        CreateSuggestedEditRequest request,
        string submittedByLogtoUserId,
        CancellationToken cancellationToken);

    Task<Result<SuggestedEditDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<SuggestedEditDto>>> ListAsync(
        SuggestedEditListFilters filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records that the Owner has accepted the suggestion. Does NOT modify
    /// the target content — the Owner separately applies the change via
    /// Translations. Caller must be <c>Owner</c>.
    /// </summary>
    Task<Result<SuggestedEditDto>> AcceptAsync(
        Guid id,
        DecisionRequest request,
        string decidedByLogtoUserId,
        CancellationToken cancellationToken);

    /// <summary>Records that the Owner has rejected the suggestion. Caller must be <c>Owner</c>.</summary>
    Task<Result<SuggestedEditDto>> RejectAsync(
        Guid id,
        DecisionRequest request,
        string decidedByLogtoUserId,
        CancellationToken cancellationToken);
}
