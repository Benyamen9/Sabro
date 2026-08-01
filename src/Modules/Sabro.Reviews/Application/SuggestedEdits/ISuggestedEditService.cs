using Sabro.Reviews.Domain;
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

    /// <summary>
    /// Files a proposed new value for one field of a Lexicon entry or a historical
    /// figure. Caller must hold the reviewer role for that area.
    /// </summary>
    /// <remarks>
    /// The target must exist and the field must be one the owning module declares
    /// proposable — which is what keeps publication state (<c>Status</c>,
    /// <c>PlayableInMeltho</c>, <c>PlayableInShmo</c>) out of reach: a reviewer
    /// cannot propose a change to something that is not on the list.
    /// </remarks>
    Task<Result<SuggestedEditDto>> ProposeFieldChangeAsync(
        CreateFieldProposalRequest request,
        string submittedByLogtoUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Which fields of <paramref name="targetType"/> a reviewer may propose against,
    /// as declared by the module that owns it.
    /// </summary>
    /// <remarks>
    /// Exists so the backoffice can build its "propose a correction" picker from the
    /// server's list instead of keeping a copy. A second copy would drift, and the
    /// drift would be silent: the picker would offer a field the API then refuses, or
    /// hide one it would have accepted.
    /// </remarks>
    Result<IReadOnlyCollection<string>> GetProposableFields(SuggestedEditTargetType targetType);

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
