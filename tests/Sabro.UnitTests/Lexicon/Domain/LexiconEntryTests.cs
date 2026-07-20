using Sabro.Lexicon.Domain;

namespace Sabro.UnitTests.Lexicon.Domain;

public class LexiconEntryTests
{
    private const string KtbUnvocalized = "ܟܬܒ";
    private const string KtbVocalized = "ܟܬ݂ܳܒ݂";

    private static readonly string[] TwoVariants = { "kthab", "ktab" };

    private static readonly string[] MessyVariants = { "  kthab  ", string.Empty, "  ", "ktab" };

    [Fact]
    public void Create_WithMinimalValidInput_ReturnsSuccess()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        result.Value.SyriacVocalized.Should().BeNull();
        result.Value.RootId.Should().BeNull();
        result.Value.SblTransliteration.Should().Be("ktb");
        result.Value.TransliterationVariants.Should().BeEmpty();
        result.Value.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
        result.Value.Morphology.Should().BeNull();
        result.Value.Meanings.Should().BeEmpty();
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.Status.Should().Be(LexiconEntryStatus.Draft);
        result.Value.PlayableInMeltho.Should().BeFalse();
        result.Value.PlayableLength.Should().Be(3);
    }

    [Fact]
    public void Create_WithAllFields_StoresAllFields()
    {
        var rootId = Guid.NewGuid();
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "to write").Value!,
            LexiconMeaning.Create("fr", "écrire").Value!,
        };

        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: KtbVocalized,
            rootId: rootId,
            transliterationVariants: TwoVariants,
            morphology: "Pe'al, transitive",
            meanings: meanings);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacVocalized.Should().Be(KtbVocalized);
        result.Value.RootId.Should().Be(rootId);
        result.Value.TransliterationVariants.Should().BeEquivalentTo(TwoVariants);
        result.Value.Morphology.Should().Be("Pe'al, transitive");
        result.Value.Meanings.Should().BeEquivalentTo(meanings, options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingSyriacUnvocalized_ReturnsValidationFailure(string? syriac)
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: syriac!,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithLatinSyriacUnvocalized_ReturnsValidationFailure()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: "ktb",
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithLatinSyriacVocalized_ReturnsValidationFailure()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: "ktab");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutSblTransliteration_StoresNull(string? translit)
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: translit,
            grammaticalCategory: GrammaticalCategory.Verb);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SblTransliteration.Should().BeNull();
    }

    [Fact]
    public void Create_WithUndefinedGrammaticalCategory_ReturnsValidationFailure()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: (GrammaticalCategory)999);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_TrimsAllStringFields()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: $"   {KtbUnvocalized}   ",
            sblTransliteration: "  ktb  ",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: $"   {KtbVocalized}   ",
            morphology: "  Pe'al  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        result.Value.SyriacVocalized.Should().Be(KtbVocalized);
        result.Value.SblTransliteration.Should().Be("ktb");
        result.Value.Morphology.Should().Be("Pe'al");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankOptionalStringFields_StoresNull(string blank)
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: blank,
            morphology: blank);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacVocalized.Should().BeNull();
        result.Value.Morphology.Should().BeNull();
    }

    [Fact]
    public void Create_NormalizesSyriacFieldsToNfc()
    {
        var nfdUnvocalized = KtbUnvocalized.Normalize(System.Text.NormalizationForm.FormD);
        var nfdVocalized = KtbVocalized.Normalize(System.Text.NormalizationForm.FormD);

        var result = LexiconEntry.Create(
            syriacUnvocalized: nfdUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            syriacVocalized: nfdVocalized);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacUnvocalized.IsNormalized(System.Text.NormalizationForm.FormC).Should().BeTrue();
        result.Value.SyriacVocalized!.IsNormalized(System.Text.NormalizationForm.FormC).Should().BeTrue();
    }

    [Fact]
    public void Create_DropsBlankTransliterationVariantsAndTrimsTheRest()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            transliterationVariants: MessyVariants);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransliterationVariants.Should().BeEquivalentTo(
            TwoVariants,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Create_WithEmptyRootId_ReturnsValidationFailure()
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            rootId: Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_StampsCreatedAndUpdatedTimestamps()
    {
        var before = DateTimeOffset.UtcNow;

        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb);

        var after = DateTimeOffset.UtcNow;
        result.IsSuccess.Should().BeTrue();
        result.Value!.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        result.Value.UpdatedAt.Should().Be(result.Value.CreatedAt);
    }

    [Fact]
    public void Publish_WithAllRequiredGlosses_SetsPublished()
    {
        var entry = CreateDraftWithAllMeanings();

        var error = entry.Publish();

        error.Should().BeNull();
        entry.Status.Should().Be(LexiconEntryStatus.Published);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("nl")]
    [InlineData("de")]
    [InlineData("sv")]
    public void Publish_WithMissingRequiredGloss_ReturnsValidationAndStaysDraft(string missing)
    {
        var meanings = AllFiveMeanings().Where(m => m.Language != missing).ToArray();
        var entry = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            meanings: meanings).Value!;

        var error = entry.Publish();

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        entry.Status.Should().Be(LexiconEntryStatus.Draft);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_IsIdempotent()
    {
        var entry = CreateDraftWithAllMeanings();
        entry.Publish();

        var error = entry.Publish();

        error.Should().BeNull();
        entry.Status.Should().Be(LexiconEntryStatus.Published);
    }

    [Fact]
    public void SetPlayable_OnDraftEntry_ReturnsConflict()
    {
        var entry = CreateDraftWithAllMeanings();

        var error = entry.SetPlayable(true);

        error.Should().NotBeNull();
        error!.Code.Should().Be("conflict");
        entry.PlayableInMeltho.Should().BeFalse();
    }

    [Fact]
    public void SetPlayable_OnPublishedEntry_SetsFlag()
    {
        var entry = CreateDraftWithAllMeanings();
        entry.Publish();

        var error = entry.SetPlayable(true);

        error.Should().BeNull();
        entry.PlayableInMeltho.Should().BeTrue();
    }

    [Fact]
    public void SetPlayable_DoesNotEnforceLengthWindow()
    {
        // A single-letter word is outside the 2–8 window but can still be flagged;
        // the eligible-pool predicate (not the flag) enforces the bound.
        var entry = LexiconEntry.Create(
            syriacUnvocalized: "ܐ",
            sblTransliteration: "ʾ",
            grammaticalCategory: GrammaticalCategory.Other,
            meanings: AllFiveMeanings()).Value!;
        entry.Publish();

        var error = entry.SetPlayable(true);

        error.Should().BeNull();
        entry.PlayableInMeltho.Should().BeTrue();
        entry.PlayableLength.Should().Be(1);
    }

    [Fact]
    public void ReturnToDraft_ClearsPublishedAndPlayable()
    {
        var entry = CreateDraftWithAllMeanings();
        entry.Publish();
        entry.SetPlayable(true);

        entry.ReturnToDraft();

        entry.Status.Should().Be(LexiconEntryStatus.Draft);
        entry.PlayableInMeltho.Should().BeFalse();
    }

    [Fact]
    public void Update_RecomputesPlayableLength()
    {
        var entry = CreateDraftWithAllMeanings();

        var error = entry.Update(
            syriacUnvocalized: "ܐܒ",
            sblTransliteration: "ʾb",
            grammaticalCategory: GrammaticalCategory.Noun,
            meanings: AllFiveMeanings());

        error.Should().BeNull();
        entry.SyriacUnvocalized.Should().Be("ܐܒ");
        entry.PlayableLength.Should().Be(2);
    }

    [Fact]
    public void Update_OnDraftEntry_AllowsPartialMeanings()
    {
        var entry = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb).Value!;

        var error = entry.Update(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            meanings: new[] { LexiconMeaning.Create("en", "to write").Value! });

        error.Should().BeNull();
        entry.Meanings.Should().ContainSingle();
    }

    [Fact]
    public void Update_OnPublishedEntry_DroppingRequiredGloss_ReturnsValidationAndLeavesEntryUnchanged()
    {
        var entry = CreateDraftWithAllMeanings();
        entry.Publish();

        var error = entry.Update(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            meanings: new[] { LexiconMeaning.Create("en", "to write").Value! });

        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        entry.Status.Should().Be(LexiconEntryStatus.Published);
        entry.Meanings.Should().HaveCount(5);
    }

    [Fact]
    public void Update_BumpsUpdatedAt()
    {
        var entry = CreateDraftWithAllMeanings();
        var original = entry.UpdatedAt;

        var error = entry.Update(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: "ktb",
            grammaticalCategory: GrammaticalCategory.Verb,
            meanings: AllFiveMeanings());

        error.Should().BeNull();
        entry.UpdatedAt.Should().BeOnOrAfter(original);
    }

    private static LexiconMeaning[] AllFiveMeanings() => new[]
    {
        LexiconMeaning.Create("en", "to write").Value!,
        LexiconMeaning.Create("fr", "écrire").Value!,
        LexiconMeaning.Create("nl", "schrijven").Value!,
        LexiconMeaning.Create("de", "schreiben").Value!,
        LexiconMeaning.Create("sv", "skriva").Value!,
    };

    private static LexiconEntry CreateDraftWithAllMeanings() => LexiconEntry.Create(
        syriacUnvocalized: KtbUnvocalized,
        sblTransliteration: "ktb",
        grammaticalCategory: GrammaticalCategory.Verb,
        meanings: AllFiveMeanings()).Value!;
}
