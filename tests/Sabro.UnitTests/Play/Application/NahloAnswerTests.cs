using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Public;
using Sabro.Play.Application.Nahlo;

namespace Sabro.UnitTests.Play.Application;

/// <summary>
/// Sabro's copy of Nahlo's scoring rule, used to check a claimed win: melody and
/// mode (when the chant has one) by normalised text, and "is it a shuḥlofo?".
/// </summary>
public class NahloAnswerTests
{
    [Fact]
    public void IsRight_WithEveryPartRight_IsTrue()
    {
        NahloAnswer.IsRight(new NahloGuess("Zodeq dnehwe", "Tlithoyo", false), Chant()).Should().BeTrue();
    }

    [Fact]
    public void IsRight_IgnoresCaseSpaceAndComposition()
    {
        NahloAnswer.IsRight(new NahloGuess("  ZODEQ DNEHWE ", "tlithoyo", false), Chant()).Should().BeTrue();
    }

    [Theory]
    [InlineData("Mo rhimin", "Tlithoyo", false)]
    [InlineData("Zodeq dnehwe", "Qadmoyo", false)]
    [InlineData("Zodeq dnehwe", "Tlithoyo", true)]
    public void IsRight_WithAnyPartWrong_IsFalse(string melody, string mode, bool isShuhlofo)
    {
        NahloAnswer.IsRight(new NahloGuess(melody, mode, isShuhlofo), Chant()).Should().BeFalse();
    }

    [Fact]
    public void IsRight_ForAChantWithoutMode_DoesNotAskTheMode()
    {
        var madrosho = Chant(modeName: null);

        NahloAnswer.IsRight(new NahloGuess("Zodeq dnehwe", string.Empty, false), madrosho).Should().BeTrue();
    }

    [Fact]
    public void IsRight_AHrinoAnswersNoToTheShuhlofoQuestion()
    {
        var hrino = Chant(variantKind: ChantVariantKind.Hrino);

        NahloAnswer.IsRight(new NahloGuess("Zodeq dnehwe", "Tlithoyo", false), hrino).Should().BeTrue();
        NahloAnswer.IsRight(new NahloGuess("Zodeq dnehwe", "Tlithoyo", true), hrino).Should().BeFalse();
    }

    [Fact]
    public void IsRight_AShuhlofoAnswersYes()
    {
        var shuhlofo = Chant(variantKind: ChantVariantKind.Shuhlofo);

        NahloAnswer.IsRight(new NahloGuess("Zodeq dnehwe", "Tlithoyo", true), shuhlofo).Should().BeTrue();
    }

    [Fact]
    public void Guesses_ReadsTheClientsDetailInOrder()
    {
        var detail = """
            {"guesses":[{"melody":"Mo rhimin","mode":"Qadmoyo","isShuhlofo":false},
                        {"melody":"Zodeq dnehwe","mode":"Tlithoyo","isShuhlofo":true}],
             "elapsedSeconds":42,"audioPlayed":true}
            """;

        NahloResultDetail.Guesses(detail).Should().Equal(
            new NahloGuess("Mo rhimin", "Qadmoyo", false),
            new NahloGuess("Zodeq dnehwe", "Tlithoyo", true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("{\"guesses\":\"nope\"}")]
    [InlineData("{\"guesses\":[{\"mode\":\"Qadmoyo\"}]}")]
    public void Guesses_FromAMissingOrMalformedDetail_IsEmpty(string? detail)
    {
        NahloResultDetail.Guesses(detail).Should().BeEmpty();
    }

    private static PlayableChant Chant(string? modeName = "Tlithoyo", ChantVariantKind variantKind = ChantVariantKind.None) =>
        new(
            Guid.NewGuid(),
            null,
            null,
            "Zodeq dnehwe",
            "Qole shahroye",
            modeName,
            variantKind,
            variantKind == ChantVariantKind.None ? null : 1,
            "/media/chants/x.mp3");
}
