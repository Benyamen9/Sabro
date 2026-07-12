using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class MnoEquationGeneratorTests
{
    [Fact]
    public void Generate_SatisfiesEveryBoardInvariant()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var equation = MnoEquationGenerator.Generate(new Random(seed));

            var (numbers, operators) = Tokenize(equation.Expression);

            operators.Count.Should().BeInRange(1, 2, because: "seed {0} must use 1-2 operators", seed);
            numbers.Should().OnlyContain(n => n >= 1 && n <= 999, because: "seed {0} operands stay in the unmarked range", seed);

            var tiles = numbers.Sum(SyriacNumerals.TileCount) + operators.Count;
            tiles.Should().Be(MnoEquationGenerator.TileWidth, because: "seed {0} must fill the board exactly", seed);

            Evaluate(numbers, operators).Should().Be(equation.Target, because: "seed {0} expression must equal its target", seed);
            equation.Target.Should().BeGreaterThanOrEqualTo(1);

            AssertNoDegenerateSteps(numbers, operators, seed);

            equation.TileForm.Should().Be(ExpectedTileForm(numbers, operators), because: "seed {0} tile form is the spelled expression", seed);
        }
    }

    [Fact]
    public void Generate_AvoidsExcludedExpressions()
    {
        var first = MnoEquationGenerator.Generate(new Random(42));
        var retry = MnoEquationGenerator.Generate(new Random(42), new HashSet<string> { first.Expression });

        retry.Expression.Should().NotBe(first.Expression);
    }

    [Fact]
    public void Generate_ProducesVariety()
    {
        var expressions = new HashSet<string>();
        for (var seed = 0; seed < 200; seed++)
        {
            expressions.Add(MnoEquationGenerator.Generate(new Random(seed)).Expression);
        }

        expressions.Count.Should().BeGreaterThan(150, because: "the space is enormous; near-duplicates would signal a biased picker");
    }

    [Fact]
    public void Generate_UsesAllFourOperatorsAcrossSeeds()
    {
        var seen = new HashSet<char>();
        for (var seed = 0; seed < 300; seed++)
        {
            foreach (var op in MnoEquationGenerator.Generate(new Random(seed)).Expression.Where(c => c is '+' or '-' or '*' or '/'))
            {
                seen.Add(op);
            }
        }

        seen.Should().BeEquivalentTo(['+', '-', '*', '/']);
    }

    private static (List<int> Numbers, List<char> Operators) Tokenize(string expression)
    {
        var numbers = new List<int>();
        var operators = new List<char>();
        var current = string.Empty;
        foreach (var ch in expression)
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
        return (numbers, operators);
    }

    // Standard precedence, left to right: products/quotients collapse first,
    // and every division along the way must be exact.
    private static int Evaluate(List<int> numbers, List<char> operators)
    {
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

    private static string ExpectedTileForm(List<int> numbers, List<char> operators)
    {
        var form = SyriacNumerals.Spell(numbers[0]);
        for (var i = 0; i < operators.Count; i++)
        {
            form += operators[i] + SyriacNumerals.Spell(numbers[i + 1]);
        }

        return form;
    }
}
