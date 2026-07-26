using System.Text;

namespace Sabro.Play.Domain;

/// <summary>
/// Generates the daily Mno equation by rejection sampling: pick a shape
/// (operator count, operators, per-number tile lengths), pick numbers of those
/// tile lengths, and keep the draw only when it satisfies every board rule —
/// the level's exact tile width, an integer target inside the level's band,
/// every division exact, and no degenerate steps (multiplying or dividing
/// with 1, dividing a number by itself).
///
/// Difficulty (owner-recalibrated ladder, 2026-07-15) shapes the draw: the
/// board width, how many operators and which, whether a × or ÷ is guaranteed,
/// the operand range, how big the target may get, and which spelling the tile
/// form uses — canonical up to Hard (alfayo enters naturally with the
/// thousands), the compact marked spelling on Extreme so all four multiplier
/// marks are met in play. The widths climb the ladder (3/4/5/6/6) so each
/// level scales puzzle depth, not just numeral length.
///
/// Normal and above may also group one adjacent operand pair in parentheses
/// when the two operators are mixed precedence (a +/- paired with a */) —
/// grouping is the only thing that changes the result in that case, since
/// same-tier operator pairs already evaluate left to right either way. This
/// is deliberately bounded to a single, non-nested pair (never more, given
/// the ladder never draws more than 2 operators) and costs 2 tiles on top of
/// the level's normal width, so it only widens the board on the days it's
/// actually used.
/// </summary>
public static class MnoEquationGenerator
{
    private const int MaxAttempts = 100_000;

    private static readonly Dictionary<MnoDifficulty, DifficultyProfile> Profiles = new()
    {
        // Width 3 with one operator forces two single-letter numbers; the
        // operand cap at 90 keeps even the draw pool to units and tens.
        [MnoDifficulty.Beginner] = new(Width: 3, MaxOperand: 90, Operators: ['+', '-'], MaxTarget: 180, OperatorCounts: [1], RequireMulOrDiv: false, RequireOperandAtLeast: 0, SyriacNumerals.Spell, AllowParentheses: false),

        // Width 4 with one operator forces a two-letter compound plus a single letter.
        [MnoDifficulty.Easy] = new(Width: 4, MaxOperand: 99, Operators: ['+', '-'], MaxTarget: 189, OperatorCounts: [1], RequireMulOrDiv: false, RequireOperandAtLeast: 0, SyriacNumerals.Spell, AllowParentheses: false),
        [MnoDifficulty.Normal] = new(Width: 5, MaxOperand: 999, Operators: ['+', '-', '*', '/'], MaxTarget: 9_999, OperatorCounts: [1, 2], RequireMulOrDiv: true, RequireOperandAtLeast: 0, SyriacNumerals.Spell, AllowParentheses: true),
        [MnoDifficulty.Hard] = new(Width: 6, MaxOperand: 9_999, Operators: ['+', '-', '*', '/'], MaxTarget: 99_999, OperatorCounts: [2], RequireMulOrDiv: true, RequireOperandAtLeast: 1_000, SyriacNumerals.Spell, AllowParentheses: true),
        [MnoDifficulty.Extreme] = new(Width: 6, MaxOperand: 999_999, Operators: ['+', '-', '*', '/'], MaxTarget: 999_999, OperatorCounts: [2], RequireMulOrDiv: true, RequireOperandAtLeast: 10_000, SyriacNumerals.SpellMarked, AllowParentheses: true),
    };

    /// <summary>
    /// The level's normal board width in tiles. Normal/Hard/Extreme occasionally
    /// draw a parenthesized equation instead — 2 tiles wider than this, never
    /// narrower — so callers should size the board from the actual returned tile
    /// form, not assume every equation is exactly this many tiles.
    /// </summary>
    public static int WidthOf(MnoDifficulty difficulty) => Profiles[difficulty].Width;

    /// <summary>
    /// Draws a valid equation for the level. <paramref name="excludedExpressions"/>
    /// lets the caller replay-guard against recently served expressions.
    /// </summary>
    public static MnoEquation Generate(MnoDifficulty difficulty, Random random, IReadOnlySet<string>? excludedExpressions = null)
    {
        var profile = Profiles[difficulty];

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var operatorCount = profile.OperatorCounts[random.Next(profile.OperatorCounts.Length)];
            var operators = new char[operatorCount];
            for (var i = 0; i < operatorCount; i++)
            {
                operators[i] = profile.Operators[random.Next(profile.Operators.Length)];
            }

            if (profile.RequireMulOrDiv && !operators.Any(op => op is '*' or '/'))
            {
                continue;
            }

            // Grouping only ever changes anything with exactly 2 operators of
            // mixed precedence (same-tier pairs already evaluate left to right
            // the same either way) — bounded to that one shape, never nested.
            // It costs 2 tiles on top of the level's normal budget, so it only
            // widens the board on the days it's actually used.
            var groupedIndex = -1;
            if (profile.AllowParentheses && operatorCount == 2 && IsMixedPrecedence(operators[0], operators[1]) && random.Next(2) == 0)
            {
                groupedIndex = operators[0] is '+' or '-' ? 0 : 1;
            }

            // The operand-tile budget is unchanged either way — the 2 paren
            // tiles are pure overhead added on top, widening the *board*
            // (Width -> Width + 2) without taking anything from the operands.
            var lengths = SplitTiles(random, profile.Width - operatorCount, operatorCount + 1);
            if (lengths is null)
            {
                continue;
            }

            var numbers = new int[lengths.Length];
            var drawable = true;
            for (var i = 0; i < lengths.Length; i++)
            {
                if (!profile.PoolsByTileCount.TryGetValue(lengths[i], out var pool))
                {
                    drawable = false;
                    break;
                }

                numbers[i] = pool[random.Next(pool.Length)];
            }

            if (!drawable)
            {
                continue;
            }

            if (profile.RequireOperandAtLeast > 0 && !numbers.Any(n => n >= profile.RequireOperandAtLeast))
            {
                continue;
            }

            if (HasDegenerateStep(numbers, operators))
            {
                continue;
            }

            var target = groupedIndex >= 0 ? TryEvaluateGrouped(numbers, operators, groupedIndex) : TryEvaluate(numbers, operators);
            if (target is null or < 1 || target > profile.MaxTarget)
            {
                continue;
            }

            var expression = BuildExpression(numbers, operators, groupedIndex);
            if (excludedExpressions is not null && excludedExpressions.Contains(expression))
            {
                continue;
            }

            return new MnoEquation(expression, BuildTileForm(numbers, operators, profile.Spell, groupedIndex), target.Value);
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

    /// <summary>+/- and */ are different precedence tiers — grouping only ever
    /// changes the result when the two operators straddle that boundary.</summary>
    private static bool IsMixedPrecedence(char op0, char op1)
    {
        bool IsAdditive(char op) => op is '+' or '-';
        return IsAdditive(op0) != IsAdditive(op1);
    }

    /// <summary>
    /// Evaluates one of the two bounded grouped shapes Mno currently produces —
    /// (n0 op0 n1) op1 n2, or n0 op0 (n1 op1 n2) — never nested, never more than
    /// one pair. Only reached when the two operators are mixed precedence, so
    /// the grouped pair is always +/- and the outer combination always */;
    /// null when the outer division is inexact.
    /// </summary>
    private static int? TryEvaluateGrouped(int[] numbers, char[] operators, int groupedIndex)
    {
        long Apply(char op, long a, long b) => op switch
        {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            _ => a / b,
        };

        // b can be zero here even though every drawn operand is >= 1: when
        // grouped is the outer division's divisor, b is the *grouped pair's
        // result* (e.g. "7-7"), not a raw operand.
        bool IsExact(char op, long a, long b) => op != '/' || (b != 0 && a % b == 0);

        var groupOp = operators[groupedIndex];
        long a = numbers[groupedIndex];
        long b = numbers[groupedIndex + 1];
        if (!IsExact(groupOp, a, b))
        {
            return null;
        }

        var groupResult = Apply(groupOp, a, b);

        var outerIndex = groupedIndex == 0 ? 1 : 0;
        var outerOp = operators[outerIndex];
        var (left, right) = groupedIndex == 0 ? (groupResult, (long)numbers[2]) : ((long)numbers[0], groupResult);
        if (!IsExact(outerOp, left, right))
        {
            return null;
        }

        var total = Apply(outerOp, left, right);
        return total is >= int.MinValue and <= int.MaxValue ? (int)total : null;
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

    private static string BuildExpression(int[] numbers, char[] operators, int groupedIndex = -1)
    {
        if (groupedIndex < 0)
        {
            var builder = new StringBuilder().Append(numbers[0]);
            for (var i = 0; i < operators.Length; i++)
            {
                builder.Append(operators[i]).Append(numbers[i + 1]);
            }

            return builder.ToString();
        }

        // Bounded to the operatorCount == 2 shape — wrap the grouped pair,
        // then apply the outer operator to the remaining operand.
        return groupedIndex == 0
            ? $"({numbers[0]}{operators[0]}{numbers[1]}){operators[1]}{numbers[2]}"
            : $"{numbers[0]}{operators[0]}({numbers[1]}{operators[1]}{numbers[2]})";
    }

    private static string BuildTileForm(int[] numbers, char[] operators, Func<int, string> spell, int groupedIndex = -1)
    {
        if (groupedIndex < 0)
        {
            var builder = new StringBuilder().Append(spell(numbers[0]));
            for (var i = 0; i < operators.Length; i++)
            {
                builder.Append(operators[i]).Append(spell(numbers[i + 1]));
            }

            return builder.ToString();
        }

        return groupedIndex == 0
            ? $"({spell(numbers[0])}{operators[0]}{spell(numbers[1])}){operators[1]}{spell(numbers[2])}"
            : $"{spell(numbers[0])}{operators[0]}({spell(numbers[1])}{operators[1]}{spell(numbers[2])})";
    }

    /// <summary>
    /// One ladder level's draw constraints. <paramref name="RequireOperandAtLeast"/>
    /// demands at least one operand that large (0 = no floor) — without it a
    /// Hard/Extreme draw could land entirely in a lower level's range.
    /// <paramref name="RequireMulOrDiv"/> guarantees a × or ÷ on every board —
    /// without it, rejection sampling starves both exactly where they matter.
    /// </summary>
    private sealed record DifficultyProfile(
        int Width,
        int MaxOperand,
        char[] Operators,
        int MaxTarget,
        int[] OperatorCounts,
        bool RequireMulOrDiv,
        int RequireOperandAtLeast,
        Func<int, string> Spell,
        bool AllowParentheses)
    {
        /// <summary>Numbers grouped by the tile length of this level's spelling, so a draw for a slot width is O(1).</summary>
        public Dictionary<int, int[]> PoolsByTileCount { get; } = Enumerable
            .Range(1, MaxOperand)
            .GroupBy(value => SyriacNumerals.TileCountOf(Spell(value)))
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
