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
    public void Create_WithMissingSblTransliteration_ReturnsValidationFailure(string? translit)
    {
        var result = LexiconEntry.Create(
            syriacUnvocalized: KtbUnvocalized,
            sblTransliteration: translit!,
            grammaticalCategory: GrammaticalCategory.Verb);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
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
}
