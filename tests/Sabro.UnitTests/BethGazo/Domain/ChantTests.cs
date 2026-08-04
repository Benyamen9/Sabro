using Sabro.BethGazo.Domain;

namespace Sabro.UnitTests.BethGazo.Domain;

public class ChantTests
{
    /// <summary>ܡܪܝܡ — a Syriac incipit standing in for the real one.</summary>
    private const string Incipit = "ܡܪܝܡ";

    private static readonly Guid Tlithoyo = Guid.NewGuid();

    [Fact]
    public void Create_StartsAsAnUnplayableDraft()
    {
        var chant = CreateChant();

        chant.Status.Should().Be(ChantStatus.Draft);
        chant.PlayableInNahlo.Should().BeFalse();
        chant.AudioUrl.Should().BeNull();
    }

    [Fact]
    public void Create_NormalizesAndTrimsTheSyriacAndTheTransliteration()
    {
        var chant = CreateChant(incipit: $"  {Incipit}  ", transliteration: "  Maryam yoldath Aloho  ");

        chant.SyriacIncipit.Should().Be(Incipit);
        chant.Transliteration.Should().Be("Maryam yoldath Aloho");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAnIncipit_Fails(string incipit)
    {
        var result = Chant.Create(incipit, "Maryam yoldath Aloho", Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("SyriacIncipit");
    }

    [Fact]
    public void Create_WithNonSyriacIncipit_Fails()
    {
        var result = Chant.Create("Maryam", "Maryam yoldath Aloho", Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Syriac");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutATransliteration_Fails(string transliteration)
    {
        // Required rather than optional enrichment: it is the name a player types.
        var result = Chant.Create(Incipit, transliteration, Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Transliteration");
    }

    [Fact]
    public void Create_WithoutAMode_Fails()
    {
        var result = Chant.Create(Incipit, "Maryam yoldath Aloho", Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("mode");
    }

    [Fact]
    public void Create_LeavesTheShuhlofoNullWhenThereIsNone()
    {
        // Only some melodies have one, so absent is a real state rather than a blank.
        CreateChant().Shuhlofo.Should().BeNull();
        CreateChant(shuhlofo: "   ").Shuhlofo.Should().BeNull();
        CreateChant(shuhlofo: "  second  ").Shuhlofo.Should().Be("second");
    }

    [Fact]
    public void Publish_WithoutARecording_Fails()
    {
        // The publish gate: no audio means no puzzle.
        var chant = CreateChant();

        var error = chant.Publish();

        error.Should().NotBeNull();
        error!.Message.Should().Contain("recording");
        chant.Status.Should().Be(ChantStatus.Draft);
    }

    [Fact]
    public void Publish_WithARecording_Succeeds_AndIsIdempotent()
    {
        var chant = CreateChant();
        chant.SetAudioUrl("/media/chants/maryam.m4a").Should().BeNull();

        chant.Publish().Should().BeNull();
        chant.Status.Should().Be(ChantStatus.Published);

        chant.Publish().Should().BeNull();
        chant.Status.Should().Be(ChantStatus.Published);
    }

    [Fact]
    public void SetAudioUrl_CannotClearTheRecordingOfAPublishedChant()
    {
        // Clearing it would leave a published chant unplayable while still in the
        // pool — return it to draft instead.
        var chant = CreateChant();
        chant.SetAudioUrl("/media/chants/maryam.m4a");
        chant.Publish();

        var error = chant.SetAudioUrl(null);

        error.Should().NotBeNull();
        chant.AudioUrl.Should().Be("/media/chants/maryam.m4a");
    }

    [Fact]
    public void SetPlayable_RequiresPublication()
    {
        var chant = CreateChant();

        var error = chant.SetPlayable(true);

        error.Should().NotBeNull();
        chant.PlayableInNahlo.Should().BeFalse();
    }

    [Fact]
    public void ReturnToDraft_ClearsThePlayableFlag()
    {
        var chant = CreateChant();
        chant.SetAudioUrl("/media/chants/maryam.m4a");
        chant.Publish();
        chant.SetPlayable(true).Should().BeNull();

        chant.ReturnToDraft();

        chant.Status.Should().Be(ChantStatus.Draft);
        chant.PlayableInNahlo.Should().BeFalse();
    }

    [Fact]
    public void Update_RejectsAChantInheritingItsOwnMelody()
    {
        // A solqin points at the chant whose melody it borrows; pointing at itself
        // is a cycle of one.
        var chant = CreateChant();

        var error = chant.Update(Incipit, "Maryam yoldath Aloho", Tlithoyo, inheritsMelodyFromId: chant.Id);

        error.Should().NotBeNull();
        error!.Message.Should().Contain("own melody");
    }

    [Fact]
    public void Update_AcceptsASolqinPointingAtAnotherChant()
    {
        var parent = CreateChant();
        var solqin = CreateChant(transliteration: "Another melody");

        var error = solqin.Update(
            Incipit,
            "Another melody",
            Tlithoyo,
            inheritsMelodyFromId: parent.Id);

        error.Should().BeNull();
        solqin.InheritsMelodyFromId.Should().Be(parent.Id);
    }

    [Fact]
    public void Update_DoesNotTouchStatusPlayabilityOrAudio()
    {
        var chant = CreateChant();
        chant.SetAudioUrl("/media/chants/maryam.m4a");
        chant.Publish();
        chant.SetPlayable(true);

        chant.Update(Incipit, "Renamed melody", Tlithoyo).Should().BeNull();

        chant.Transliteration.Should().Be("Renamed melody");
        chant.Status.Should().Be(ChantStatus.Published);
        chant.PlayableInNahlo.Should().BeTrue();
        chant.AudioUrl.Should().Be("/media/chants/maryam.m4a");
    }

    private static Chant CreateChant(
        string incipit = Incipit,
        string transliteration = "Maryam yoldath Aloho",
        Guid? modeId = null,
        string? shuhlofo = null)
    {
        var result = Chant.Create(incipit, transliteration, modeId ?? Tlithoyo, shuhlofo: shuhlofo);
        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        return result.Value!;
    }
}
