using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.GameResults;

public interface IGameResultService
{
    /// <summary>
    /// Records the player's result for one game on one day. Idempotent on the
    /// (user, game, day) key: if a result already exists for that day it is
    /// returned unchanged with <see cref="RecordGameResultOutcome.WasCreated"/>
    /// false — the first result of the day stands and is never overwritten.
    /// </summary>
    Task<Result<RecordGameResultOutcome>> RecordAsync(string logtoUserId, RecordGameResultRequest request, CancellationToken cancellationToken);

    /// <summary>Returns the player's own results, newest day first, paged.</summary>
    Task<Result<PagedResult<GameResultDto>>> ListForUserAsync(string logtoUserId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Returns every result the player owns, oldest day first, unpaged. Used by
    /// the personal-data export (right to data portability), which must be
    /// complete rather than a page.
    /// </summary>
    Task<Result<IReadOnlyList<GameResultDto>>> ListAllForUserAsync(string logtoUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes every result the player owns, returning the number of
    /// rows removed. Used by account deletion (right to erasure).
    /// </summary>
    Task<Result<int>> DeleteAllForUserAsync(string logtoUserId, CancellationToken cancellationToken);
}
