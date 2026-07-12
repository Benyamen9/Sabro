using System.Text;

namespace Sabro.Play.Domain;

/// <summary>
/// Generates the daily Mno equation by rejection sampling: pick a shape
/// (operator count, operators, per-number tile lengths), pick numbers of those
/// tile lengths, and keep the draw only when it satisfies every board rule —
/// exactly six tiles, an integer target of at least 1, every division exact,
/// and no degenerate steps (multiplying or dividing with 1, dividing a number
/// by itself). Operands stay in the unmarked range (1–999) at launch; how often
/// solutions venture higher is generator tuning, not a system cap.
/// </summary>
public static class MnoEquationGenerator
{
    /// <summary>The fixed Mno board width: every equation renders to exactly this many tiles.</summary>
    public const int TileWidth = 6;

    private const int MaxOperandValue = 999;
    private const int MaxAttempts = 100_000;
    private static readonly char[] Operators = ['+', '-', '*', '/'];

    /// <summary>Numbers 1-999 grouped by canonical tile length, so a draw for a given slot width is O(1).</summary>
    private static readonly Dictionary<int, int[]> NumbersByTileCount = Enumerable
        .Range(1, MaxOperandValue)
        .GroupBy(SyriacNumerals.TileCount)
        .ToDictionary(g => g.Key, g => g.ToArray());

    /// <summary>
    /// Draws a valid equation. <paramref name="excludedExpressions"/> lets the
    /// caller replay-guard against recently served expressions.
    /// </summary>
    public static MnoEquation Generate(Random random, IReadOnlySet<string>? excludedExpressions = null)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var operatorCount = random.Next(1, 3);
            var operators = new char[operatorCount];
            for (var i = 0; i < operatorCount; i++)
            {
                operators[i] = Operators[random.Next(Operators.Length)];
            }

            var lengths = SplitTiles(random, TileWidth - operatorCount, operatorCount + 1);
            if (lengths is null)
            {
                continue;
            }

            var numbers = new int[lengths.Length];
            for (var i = 0; i < lengths.Length; i++)
            {
                var pool = NumbersByTileCount[lengths[i]];
                numbers[i] = pool[random.Next(pool.Length)];
            }

            if (HasDegenerateStep(numbers, operators))
            {
                continue;
            }

            var target = TryEvaluate(numbers, operators);
            if (target is null or < 1)
            {
                continue;
            }

            var expression = BuildExpression(numbers, operators);
            if (excludedExpressions is not null && excludedExpressions.Contains(expression))
            {
                continue;
            }

            return new MnoEquation(expression, BuildTileForm(numbers, operators), target.Value);
        }

        throw new InvalidOperationException("Mno equation generation exhausted its attempts — the constraints have become unsatisfiable.");
    }

    /// <summary>Random composition of <paramref name="total"/> tiles into <paramref name="parts"/> number slots, each a drawable length.</summary>
    private static int[]? SplitTiles(Random random, int total, int parts)
    {
        var lengths = new int[parts];
        var remaining = total;
        for (var i = 0; i < parts - 1; i++)
        {
            var max = Math.Min(remaining - (parts - 1 - i), 5);
            lengths[i] = random.Next(1, max + 1);
            remaining -= lengths[i];
        }

        if (remaining is < 1 or > 5)
        {
            return null;
        }

        lengths[^1] = remaining;
        return lengths;
    }

    private static bool HasDegenerateStep(int[] numbers, char[] operators)
    {
        for (var i = 0; i < operators.Length; i++)
        {
            if (operators[i] is '*' or '/' && (numbers[i] == 1 || numbers[i + 1] == 1))
            {
                return true;
            }

            if (operators[i] is '/' && numbers[i] == numbers[i + 1])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Standard precedence, left to right; null when any division is inexact.</summary>
    private static int? TryEvaluate(int[] numbers, char[] operators)
    {
        var terms = new List<long> { numbers[0] };
        var signs = new List<int> { 1 };
        for (var i = 0; i < operators.Length; i++)
        {
            long next = numbers[i + 1];
            switch (operators[i])
            {
                case '*':
                    terms[^1] *= next;
                    break;
                case '/':
                    if (terms[^1] % next != 0)
                    {
                        return null;
                    }

                    terms[^1] /= next;
                    break;
                case '+':
                    terms.Add(next);
                    signs.Add(1);
                    break;
                default:
                    terms.Add(next);
                    signs.Add(-1);
                    break;
            }
        }

        long total = 0;
        for (var i = 0; i < terms.Count; i++)
        {
            total += signs[i] * terms[i];
        }

        return total is >= int.MinValue and <= int.MaxValue ? (int)total : null;
    }

    private static string BuildExpression(int[] numbers, char[] operators)
    {
        var builder = new StringBuilder().Append(numbers[0]);
        for (var i = 0; i < operators.Length; i++)
        {
            builder.Append(operators[i]).Append(numbers[i + 1]);
        }

        return builder.ToString();
    }

    private static string BuildTileForm(int[] numbers, char[] operators)
    {
        var builder = new StringBuilder().Append(SyriacNumerals.Spell(numbers[0]));
        for (var i = 0; i < operators.Length; i++)
        {
            builder.Append(operators[i]).Append(SyriacNumerals.Spell(numbers[i + 1]));
        }

        return builder.ToString();
    }
}
