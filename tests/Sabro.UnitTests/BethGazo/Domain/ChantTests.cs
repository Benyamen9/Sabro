using Sabro.BethGazo.Domain;

namespace Sabro.UnitTests.BethGazo.Domain;

public class ChantTests
{
    /// <summary>ܡܪܝܡ — a Syriac incipit standing in for the real one.</summary>
    private const string Incipit = "ܡܪܝܡ";

    private static readonly Guid Tlithoyo = Guid.NewGuid();
    private static readonly Guid Mshahelfotho = Guid.NewGuid();

    /// <summary>A section that admits modes — the farde, which admit the mshaḥelfotho too.</summary>
    private static readonly BethGazoSection Farde = Section("Farde", 1, Tlithoyo, Mshahelfotho);

    /// <summary>
    /// A section that admits none. The madroshe: the owner's rule that they have no
    /// mode is expressed here as an empty allowed set, not as a flag.
    /// </summary>
    private static readonly BethGazoSection Madroshe = Section("Madroshe", 3);

    /// <summary>A section admitting the eight but not the mshaḥelfotho, which is the farde's alone.</summary>
    private static readonly BethGazoSection Gnize = Section("Gnize", 2, Tlithoyo);

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
        var result = Chant.Create(incipit, "Maryam yoldath Aloho", Farde, Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("SyriacIncipit");
    }

    [Fact]
    public void Create_WithNonSyriacIncipit_Fails()
    {
        var result = Chant.Create("Maryam", "Maryam yoldath Aloho", Farde, Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Syriac");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutATransliteration_Fails(string transliteration)
    {
        // Required rather than optional enrichment: it is the name a player types.
        var result = Chant.Create(Incipit, transliteration, Farde, Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Transliteration");
    }

    [Fact]
    public void Create_InASectionWithModes_WithoutAMode_Fails()
    {
        var result = Chant.Create(Incipit, "Maryam yoldath Aloho", Farde, modeId: null);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("mode is required");
    }

    [Fact]
    public void Create_InAModelessSection_WithoutAMode_Succeeds()
    {
        // The owner's rule, 2026-08-08: "when you choose madroshe, there is no mode
        // for them." A null mode here is the answer, not a gap.
        var result = Chant.Create(Incipit, "A madrosho", Madroshe, modeId: null);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.ModeId.Should().BeNull();
    }

    [Fact]
    public void Create_InAModelessSection_WithAMode_Fails()
    {
        // The other direction matters as much: without this, an editor could file a
        // madrosho under Tlithoyo and the round would ask a question the tradition
        // does not.
        var result = Chant.Create(Incipit, "A madrosho", Madroshe, Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("no mode");
    }

    [Fact]
    public void Create_WithAModeTheSectionDoesNotAdmit_Fails()
    {
        // The mshaḥelfotho belongs to the farde alone (owner, 2026-08-08). Gnize
        // admit the ordinals but not it.
        var result = Chant.Create(Incipit, "Maryam yoldath Aloho", Gnize, Mshahelfotho);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("not one of the modes");
    }

    [Fact]
    public void Create_WithTheMshahelfothoInTheFarde_Succeeds()
    {
        var result = Chant.Create(Incipit, "Maryam yoldath Aloho", Farde, Mshahelfotho);

        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        result.Value!.ModeId.Should().Be(Mshahelfotho);
    }

    [Fact]
    public void Create_WithoutASection_Fails()
    {
        var result = Chant.Create(Incipit, "Maryam yoldath Aloho", section: null!, Tlithoyo);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("section is required");
    }

    [Fact]
    public void Update_MovingAChantIntoAModelessSection_RequiresDroppingTheMode()
    {
        // Refiling a fardo as a madrosho without clearing its mode has to fail:
        // otherwise the chant keeps a mode its section says cannot exist.
        var chant = CreateChant();

        var kept = chant.Update(Incipit, "Maryam yoldath Aloho", Madroshe, Tlithoyo);
        kept.Should().NotBeNull();

        var dropped = chant.Update(Incipit, "Maryam yoldath Aloho", Madroshe, modeId: null);
        dropped.Should().BeNull();
        chant.ModeId.Should().BeNull();
        chant.SectionId.Should().Be(Madroshe.Id);
    }

    [Fact]
    public void Create_LeavesTheVariantNullForAChantInItsOwnRight()
    {
        // Absent is a real state, not a blank: most chants are the principal entry
        // rather than an extra standing beside it.
        var principal = CreateChant();
        principal.VariantKind.Should().Be(ChantVariantKind.None);
        principal.VariantNumber.Should().BeNull();
    }

    [Theory]
    [InlineData(ChantVariantKind.Shuhlofo)]
    [InlineData(ChantVariantKind.Hrino)]
    public void Create_KeepsTheKindAndNumberTogether(ChantVariantKind kind)
    {
        // Owner, 2026-08-08: "not all the extra chants of a mode are shuhlofe, but
        // hrone as well." A shuḥlofo varies the melody; a ḥrino is simply another
        // chant in the same mode. Both are numbered, and both must be distinguishable.
        var chant = CreateChant(variantKind: kind, variantNumber: 2);
        chant.VariantKind.Should().Be(kind);
        chant.VariantNumber.Should().Be(2);
    }

    [Fact]
    public void Create_WithAKindButNoNumber_Fails()
    {
        var result = Chant.Create(
            Incipit, "Maryam yoldath Aloho", Farde, Tlithoyo, variantKind: ChantVariantKind.Hrino);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("needs a number");
    }

    [Fact]
    public void Create_WithANumberButNoKind_Fails()
    {
        // A number with no kind does not say what it is one of.
        var result = Chant.Create(
            Incipit, "Maryam yoldath Aloho", Farde, Tlithoyo, variantNumber: 2);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("carries no number");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithANumberBelowOne_Fails(int number)
    {
        var result = Chant.Create(
            Incipit,
            "Maryam yoldath Aloho",
            Farde,
            Tlithoyo,
            variantKind: ChantVariantKind.Shuhlofo,
            variantNumber: number);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("1 or greater");
    }

    [Fact]
    public void Create_AcceptsANumberPastAnyExpectedCount()
    {
        // No upper bound, on purpose. He said some chants have more than one and
        // never said how many at most — capping it would be the same mistake as
        // assuming eight modes.
        CreateChant(variantKind: ChantVariantKind.Shuhlofo, variantNumber: 12)
            .VariantNumber.Should().Be(12);
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

        var error = chant.Update(Incipit, "Maryam yoldath Aloho", Farde, Tlithoyo, inheritsMelodyFromId: chant.Id);

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
            Farde,
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

        chant.Update(Incipit, "Renamed melody", Farde, Tlithoyo).Should().BeNull();

        chant.Transliteration.Should().Be("Renamed melody");
        chant.Status.Should().Be(ChantStatus.Published);
        chant.PlayableInNahlo.Should().BeTrue();
        chant.AudioUrl.Should().Be("/media/chants/maryam.m4a");
    }

    private static Chant CreateChant(
        string incipit = Incipit,
        string transliteration = "Maryam yoldath Aloho",
        BethGazoSection? section = null,
        Guid? modeId = null,
        ChantVariantKind variantKind = ChantVariantKind.None,
        int? variantNumber = null)
    {
        var result = Chant.Create(
            incipit,
            transliteration,
            section ?? Farde,
            modeId ?? Tlithoyo,
            variantKind: variantKind,
            variantNumber: variantNumber);
        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        return result.Value!;
    }

    /// <summary>Builds a section with a fixed set of admitted modes.</summary>
    private static BethGazoSection Section(string name, int position, params Guid[] modeIds)
    {
        var result = BethGazoSection.Create(name, position);
        result.IsSuccess.Should().BeTrue(result.Error?.Message);

        var section = result.Value!;
        section.SetAllowedModes(modeIds).Should().BeNull();
        return section;
    }
}
