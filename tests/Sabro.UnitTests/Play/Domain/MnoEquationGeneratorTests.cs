using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class MnoEquationGeneratorTests
{
    [Theory]
    [InlineData(MnoDifficulty.Beginner, 3, 1, 1)]
    [InlineData(MnoDifficulty.Easy, 4, 1, 1)]
    [InlineData(MnoDifficulty.Normal, 5, 1, 2)]
    [InlineData(MnoDifficulty.Hard, 6, 2, 2)]
    [InlineData(MnoDifficulty.Extreme, 6, 2, 2)]
    public void Generate_SatisfiesEveryBoardInvariant(MnoDifficulty difficulty, int width, int minOperators, int maxOperators)
    {
        MnoEquationGenerator.WidthOf(difficulty).Should().Be(width);

        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(difficulty, new Random(seed));

            var (numbers, operators, groupedIndex) = Tokenize(equation.Expression);

            operators.Count.Should().BeInRange(minOperators, maxOperators, because: "seed {0} must use the level's operator count", seed);

            var tiles = SyriacNumerals.TileCountOf(equation.TileForm);
            var expectedWidth = groupedIndex.HasValue ? width + 2 : width;
            tiles.Should().Be(expectedWidth, because: "seed {0} must fill the level's board exactly (plus the 2 parenthesis tiles when grouped)", seed);

            Evaluate(numbers, operators, groupedIndex).Should().Be(equation.Target, because: "seed {0} expression must equal its target", seed);
            equation.Target.Should().BeGreaterThanOrEqualTo(1);

            AssertNoDegenerateSteps(numbers, operators, seed);
        }
    }

    [Theory]
    [InlineData(MnoDifficulty.Beginner)]
    [InlineData(MnoDifficulty.Easy)]
    public void Generate_BelowNormal_NeverUsesParentheses(MnoDifficulty difficulty)
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(difficulty, new Random(seed));
            equation.TileForm.Should().NotContainAny(["(", ")"], because: "seed {0}: parentheses are Normal and above only", seed);
        }
    }

    [Theory]
    [InlineData(MnoDifficulty.Normal)]
    [InlineData(MnoDifficulty.Hard)]
    [InlineData(MnoDifficulty.Extreme)]
    public void Generate_NormalAndAbove_SometimesGroupsOnlyMixedPrecedencePairs(MnoDifficulty difficulty)
    {
        var sawGrouping = false;

        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(difficulty, new Random(seed));
            var openCount = equation.TileForm.Count(c => c == '(');
            var closeCount = equation.TileForm.Count(c => c == ')');
            openCount.Should().Be(closeCount, because: "seed {0}: parentheses must be balanced", seed);
            openCount.Should().BeLessThanOrEqualTo(1, because: "seed {0}: the ladder never draws more than one grouped pair", seed);

            if (openCount == 0)
            {
                continue;
            }

            sawGrouping = true;
            var (_, operators, groupedIndex) = Tokenize(equation.Expression);
            groupedIndex.Should().NotBeNull();
            var groupOp = operators[groupedIndex!.Value];
            var otherOp = operators[groupedIndex.Value == 0 ? 1 : 0];
            (groupOp is '+' or '-').Should().Be(!(otherOp is '+' or '-'), because: "seed {0}: grouping only ever straddles the +/- vs. */ precedence boundary", seed);
        }

        sawGrouping.Should().BeTrue(because: "300 seeds should draw at least one grouped equation at this level");
    }

    [Theory]
    [InlineData(MnoDifficulty.Beginner, 90, 180, "+-")]
    [InlineData(MnoDifficulty.Easy, 99, 189, "+-")]
    [InlineData(MnoDifficulty.Normal, 999, 9_999, "+-*/")]
    [InlineData(MnoDifficulty.Hard, 9_999, 99_999, "+-*/")]
    [InlineData(MnoDifficulty.Extreme, 999_999, 999_999, "+-*/")]
    public void Generate_StaysInsideTheLevelsBand(MnoDifficulty difficulty, int maxOperand, int maxTarget, string allowedOperators)
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(difficulty, new Random(seed));
            var (numbers, operators, _) = Tokenize(equation.Expression);

            numbers.Should().OnlyContain(n => n >= 1 && n <= maxOperand, because: "seed {0} operands stay inside the level", seed);
            equation.Target.Should().BeLessThanOrEqualTo(maxTarget, because: "seed {0} target stays inside the level", seed);
            operators.Should().OnlyContain(op => allowedOperators.Contains(op), because: "seed {0} uses only the level's operators", seed);
        }
    }

    [Theory]
    [InlineData(MnoDifficulty.Normal)]
    [InlineData(MnoDifficulty.Hard)]
    [InlineData(MnoDifficulty.Extreme)]
    public void Generate_NormalAndAbove_AlwaysCarriesAMultiplicationOrDivision(MnoDifficulty difficulty)
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(difficulty, new Random(seed));
            var (_, operators, _) = Tokenize(equation.Expression);

            operators.Should().Contain(op => op == '*' || op == '/', because: "seed {0}: without × or ÷ the level plays like plain addition", seed);
        }
    }

    [Fact]
    public void Generate_Beginner_AddsOrSubtractsTwoSingleLetterNumbers()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(MnoDifficulty.Beginner, new Random(seed));
            var (numbers, _, _) = Tokenize(equation.Expression);

            numbers.Should().HaveCount(2);
            numbers.Should().OnlyContain(n => SyriacNumerals.TileCountOf(SyriacNumerals.Spell(n)) == 1, because: "seed {0}: Beginner numbers are single letters", seed);

            equation.TileForm.Should().NotContainAny("ܩ", "ܪ", "ܫ", "ܬ");
            equation.TileForm.Should().NotContainAny(SyriacNumerals.Marks.Select(m => m.ToString()).ToArray());
        }
    }

    [Fact]
    public void Generate_Easy_PairsATwoLetterCompoundWithASingleLetter()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(MnoDifficulty.Easy, new Random(seed));
            var (numbers, _, _) = Tokenize(equation.Expression);

            numbers.Should().HaveCount(2);
            numbers.Select(n => SyriacNumerals.TileCountOf(SyriacNumerals.Spell(n)))
                .Should().BeEquivalentTo([1, 2], because: "seed {0}: Easy is one compound plus one single letter", seed);
        }
    }

    [Fact]
    public void Generate_Hard_AlwaysCarriesAThousandsOperand()
    {
        for (var seed = 0; seed < 200; seed++)
        {
            var equation = MnoEquationGenerator.Generate(MnoDifficulty.Hard, new Random(seed));
            var (numbers, _, _) = Tokenize(equation.Expression);

            numbers.Should().Contain(n => n >= 1_000, because: "seed {0}: Hard without a thousands operand is just Normal", seed);
            equation.TileForm.Should().Contain(SyriacNumerals.Alfayo.ToString(), because: "seed {0}: the thousands operand spells with alfayo", seed);
        }
    }

    [Fact]
    public void Generate_Extreme_SpellsOperandsInTheCompactMarkedForm()
    {
        var marksSeen = new HashSet<char>();
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(MnoDifficulty.Extreme, new Random(seed));
            var (numbers, operators, groupedIndex) = Tokenize(equation.Expression);

            numbers.Should().Contain(n => n >= 10_000, because: "seed {0}: Extreme keeps at least one big operand", seed);
            equation.TileForm.Should().Be(ExpectedTileForm(numbers, operators, SyriacNumerals.SpellMarked, groupedIndex), because: "seed {0} tile form uses the marked spelling", seed);

            foreach (var ch in equation.TileForm.Where(SyriacNumerals.Marks.Contains))
            {
                marksSeen.Add(ch);
            }
        }

        // Across a run of days the level meets the whole multiplier system.
        marksSeen.Should().BeEquivalentTo(SyriacNumerals.Marks);
    }

    [Fact]
    public void Generate_UpToHard_SpellsOperandsCanonically()
    {
        foreach (var difficulty in new[] { MnoDifficulty.Beginner, MnoDifficulty.Easy, MnoDifficulty.Normal, MnoDifficulty.Hard })
        {
            var equation = MnoEquationGenerator.Generate(difficulty, new Random(7));
            var (numbers, operators, groupedIndex) = Tokenize(equation.Expression);

            equation.TileForm.Should().Be(ExpectedTileForm(numbers, operators, SyriacNumerals.Spell, groupedIndex));
        }
    }

    [Fact]
    public void Generate_AvoidsExcludedExpressions()
    {
        var first = MnoEquationGenerator.Generate(MnoDifficulty.Normal, new Random(42));
        var retry = MnoEquationGenerator.Generate(MnoDifficulty.Normal, new Random(42), new HashSet<string> { first.Expression });

        retry.Expression.Should().NotBe(first.Expression);
    }

    [Theory]
    [InlineData(MnoDifficulty.Beginner, 100)]
    [InlineData(MnoDifficulty.Easy, 140)]
    [InlineData(MnoDifficulty.Normal, 150)]
    [InlineData(MnoDifficulty.Hard, 150)]
    [InlineData(MnoDifficulty.Extreme, 150)]
    public void Generate_ProducesVariety(MnoDifficulty difficulty, int minimumDistinct)
    {
        var expressions = new HashSet<string>();
        for (var seed = 0; seed < 200; seed++)
        {
            expressions.Add(MnoEquationGenerator.Generate(difficulty, new Random(seed)).Expression);
        }

        expressions.Count.Should().BeGreaterThan(minimumDistinct, because: "near-duplicates would signal a biased picker or a starved pool");
    }

    [Fact]
    public void Generate_UsesAllFourOperatorsAcrossSeeds()
    {
        var seen = new HashSet<char>();
        for (var seed = 0; seed < 300; seed++)
        {
            foreach (var op in MnoEquationGenerator.Generate(MnoDifficulty.Normal, new Random(seed)).Expression.Where(c => c is '+' or '-' or '*' or '/'))
            {
                seen.Add(op);
            }
        }

        seen.Should().BeEquivalentTo(['+', '-', '*', '/']);
    }

    // Strips at most one non-nested parenthesis pair — the one bounded shape
    // MnoEquationGenerator can produce — and reports which adjacent operator
    // pair it wrapped (0 = first pair, 1 = second), so callers can re-derive
    // the intended evaluation order independently of the production code.
    private static (List<int> Numbers, List<char> Operators, int? GroupedIndex) Tokenize(string expression)
    {
        int? groupedIndex = null;
        var flat = expression;
        var openIndex = expression.IndexOf('(');
        if (openIndex >= 0)
        {
            var closeIndex = expression.IndexOf(')');
            flat = expression.Remove(closeIndex, 1).Remove(openIndex, 1);
            groupedIndex = openIndex == 0 ? 0 : 1;
        }

        var numbers = new List<int>();
        var operators = new List<char>();
        var current = string.Empty;
        foreach (var ch in flat)
        {
            if (char.IsAsciiDigit(ch))
            {
                current += ch;
                continue;
            }

            operators.Add(ch);
            numbers.Add(int.Parse(current));
            current = string.Empty;
        }

        numbers.Add(int.Parse(current));
        return (numbers, operators, groupedIndex);
    }

    private static long ApplyOperator(char op, long a, long b)
    {
        if (op == '/')
        {
            (a % b).Should().Be(0, because: "every division must be exact");
            return a / b;
        }

        return op switch
        {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }

    // Standard precedence, left to right, unless groupedIndex forces a pair to
    // evaluate first — re-derived independently of MnoEquationGenerator's own
    // grouping logic, so this can actually catch a production bug rather than
    // just agreeing with it.
    private static int Evaluate(List<int> numbers, List<char> operators, int? groupedIndex = null)
    {
        if (groupedIndex is int gi)
        {
            var groupResult = ApplyOperator(operators[gi], numbers[gi], numbers[gi + 1]);
            var outerIndex = gi == 0 ? 1 : 0;
            var (left, right) = gi == 0 ? (groupResult, (long)numbers[2]) : ((long)numbers[0], groupResult);
            return checked((int)ApplyOperator(operators[outerIndex], left, right));
        }

        var termValues = new List<long> { numbers[0] };
        var termSigns = new List<int> { 1 };
        for (var i = 0; i < operators.Count; i++)
        {
            var next = numbers[i + 1];
            switch (operators[i])
            {
                case '*':
                    termValues[^1] *= next;
                    break;
                case '/':
                    (termValues[^1] % next).Should().Be(0, because: "every division must be exact");
                    termValues[^1] /= next;
                    break;
                case '+':
                    termValues.Add(next);
                    termSigns.Add(1);
                    break;
                default:
                    termValues.Add(next);
                    termSigns.Add(-1);
                    break;
            }
        }

        long total = 0;
        for (var i = 0; i < termValues.Count; i++)
        {
            total += termSigns[i] * termValues[i];
        }

        return checked((int)total);
    }

    private static void AssertNoDegenerateSteps(List<int> numbers, List<char> operators, int seed)
    {
        for (var i = 0; i < operators.Count; i++)
        {
            if (operators[i] is '*' or '/')
            {
                numbers[i].Should().NotBe(1, because: "seed {0} must not multiply or divide with 1", seed);
                numbers[i + 1].Should().NotBe(1, because: "seed {0} must not multiply or divide with 1", seed);
            }

            if (operators[i] is '/')
            {
                numbers[i].Should().NotBe(numbers[i + 1], because: "seed {0} must not divide a number by itself", seed);
            }
        }
    }

    private static string ExpectedTileForm(List<int> numbers, List<char> operators, Func<int, string> spell, int? groupedIndex = null)
    {
        if (groupedIndex is int gi)
        {
            return gi == 0
                ? $"({spell(numbers[0])}{operators[0]}{spell(numbers[1])}){operators[1]}{spell(numbers[2])}"
                : $"{spell(numbers[0])}{operators[0]}({spell(numbers[1])}{operators[1]}{spell(numbers[2])})";
        }

        var form = spell(numbers[0]);
        for (var i = 0; i < operators.Count; i++)
        {
            form += operators[i] + spell(numbers[i + 1]);
        }

        return form;
    }
}
