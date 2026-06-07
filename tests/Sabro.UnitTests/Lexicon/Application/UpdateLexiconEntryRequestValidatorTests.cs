using FluentValidation.TestHelper;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.UnitTests.Lexicon.Application;

public class UpdateLexiconEntryRequestValidatorTests
{
    private readonly UpdateLexiconEntryRequestValidator validator = new();

    [Fact]
    public void ValidInput_HasNoErrors()
    {
        var request = new UpdateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MissingSyriacUnvocalized_HasError(string? syriac)
    {
        var request = new UpdateLexiconEntryRequest(
            SyriacUnvocalized: syriac!,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SyriacUnvocalized);
    }

    [Fact]
    public void NullSblTransliteration_IsAllowed()
    {
        var request = new UpdateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: null,
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SblTransliteration);
    }

    [Fact]
    public void UndefinedGrammaticalCategory_HasError()
    {
        var request = new UpdateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: "ktb",
            GrammaticalCategory: (GrammaticalCategory)999);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.GrammaticalCategory);
    }

    [Fact]
    public void EmptyRootId_HasError()
    {
        var request = new UpdateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb,
            RootId: Guid.Empty);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RootId!.Value);
    }
}
