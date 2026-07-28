using Sabro.Shared.Results;

namespace Sabro.Play.Application.Shmo;

public interface IShmoPuzzleService
{
    /// <summary>
    /// Returns today's Shmo puzzle, get-or-create per date: the first call for a
    /// day selects a figure from the eligible pool (excluding the anti-repetition
    /// window), records it, and returns it; every later call that day returns the
    /// same figure, so all players share one puzzle. Fails with a conflict if the
    /// eligible pool is exhausted for the day.
    /// </summary>
    Task<Result<ShmoPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken);
}
