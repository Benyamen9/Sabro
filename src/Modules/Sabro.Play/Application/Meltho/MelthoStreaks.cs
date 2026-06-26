namespace Sabro.Play.Application.Meltho;

/// <summary>
/// Pure derivation of Meltho streaks from a player's daily results. A streak is a run of
/// consecutive calendar days that were all solved; a loss or a skipped day breaks it.
/// Mirrors the client-side <c>computeMelthoStats</c> so server and client never disagree.
/// Results are one-per-day (the unique key guarantees it), so dates are treated as distinct.
/// </summary>
public static class MelthoStreaks
{
    public static MelthoStreak Compute(IEnumerable<(DateOnly PlayedOn, bool Solved)> games)
    {
        var ordered = games.OrderBy(g => g.PlayedOn).ToList();
        if (ordered.Count == 0)
        {
            return new MelthoStreak(0, 0);
        }

        // Longest historical run of consecutive solved days.
        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var game in ordered)
        {
            if (game.Solved && previous is not null && game.PlayedOn.DayNumber - previous.Value.DayNumber == 1)
            {
                run += 1;
            }
            else if (game.Solved)
            {
                run = 1;
            }
            else
            {
                run = 0;
            }

            longest = Math.Max(longest, run);
            previous = game.PlayedOn;
        }

        // Current run: trailing consecutive-day wins ending at the most recent game.
        var current = 0;
        for (var i = ordered.Count - 1; i >= 0; i--)
        {
            var game = ordered[i];
            if (!game.Solved)
            {
                break;
            }

            if (i == ordered.Count - 1)
            {
                current = 1;
            }
            else if (ordered[i + 1].PlayedOn.DayNumber - game.PlayedOn.DayNumber == 1)
            {
                current += 1;
            }
            else
            {
                break;
            }
        }

        return new MelthoStreak(longest, current);
    }
}
