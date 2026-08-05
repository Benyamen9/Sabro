using Sabro.Shared.Results;

namespace Sabro.Play.Application.Nahlo;

public interface INahloPuzzleService
{
    /// <summary>
    /// Returns today's Nahlo puzzle, get-or-create per date: the first call for a
    /// day selects a chant from the eligible pool (excluding the anti-repetition
    /// window), records it, and returns it; every later call that day returns the
    /// same chant, so all players share one puzzle. Fails with a conflict if the
    /// eligible pool is exhausted for the day.
    /// </summary>
    Task<Result<NahloPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken);
}
