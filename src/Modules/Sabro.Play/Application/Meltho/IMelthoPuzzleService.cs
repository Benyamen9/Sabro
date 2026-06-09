using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

public interface IMelthoPuzzleService
{
    /// <summary>
    /// Returns today's Meltho puzzle, get-or-create per date: the first call for a
    /// day selects a word from the eligible pool (excluding the anti-repetition
    /// window), records it, and returns it; every later call that day returns the
    /// same word, so all players share one puzzle. Fails with a conflict if the
    /// eligible pool is exhausted for the day.
    /// </summary>
    Task<Result<MelthoPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken);
}
