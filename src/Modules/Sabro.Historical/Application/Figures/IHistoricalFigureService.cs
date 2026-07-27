using Sabro.Historical.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Historical.Application.Figures;

public interface IHistoricalFigureService
{
    Task<Result<HistoricalFigureDto>> CreateAsync(CreateHistoricalFigureRequest request, CancellationToken cancellationToken);

    Task<Result<HistoricalFigureDto>> UpdateAsync(Guid id, UpdateHistoricalFigureRequest request, CancellationToken cancellationToken);

    Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<HistoricalFigureDto>> PublishAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<HistoricalFigureDto>> UnpublishAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<HistoricalFigureDto>> SetPlayableAsync(Guid id, bool playable, CancellationToken cancellationToken);

    /// <summary>Returns any figure regardless of status. For Owner/admin surfaces.</summary>
    Task<Result<HistoricalFigureDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns a figure only when it is published. For public/client surfaces.</summary>
    Task<Result<HistoricalFigureListItem>> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists figures of any status, newest first, with optional filters. For Owner/admin
    /// surfaces. Backed by a direct relational query — the roster is small enough that a
    /// dedicated search index would be plumbing without payoff.
    /// </summary>
    Task<Result<PagedResult<HistoricalFigureDto>>> ListAsync(
        string? search,
        HistoricalFigureStatus? status,
        HistoricalFigureCategory? category,
        HistoricalFigureRole? role,
        HistoricalFigureRegion? region,
        bool? playableInShmo,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists published figures alphabetically by name, for the public roster the
    /// Shmo guess-search reads. Never carries editorial state or the playable flag.
    /// </summary>
    Task<Result<PagedResult<HistoricalFigureListItem>>> ListPublishedAsync(
        HistoricalFigureCategory? category,
        HistoricalFigureRole? role,
        HistoricalFigureRegion? region,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
