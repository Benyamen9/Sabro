using FluentValidation.TestHelper;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.UnitTests.Lexicon.Application;

public class CreateLexiconEntryRequestValidatorTests
{
    private readonly CreateLexiconEntryRequestValidator validator = new();

    [Fact]
    public void ValidInput_HasNoErrors()
    {
        var request = new CreateLexiconEntryRequest(
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
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: syriac!,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SyriacUnvocalized);
    }

    [Fact]
    public void NullSblTransliteration_IsAllowed()
    {
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: null,
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SblTransliteration);
    }

    [Fact]
    public void SblTransliterationLongerThan128_HasError()
    {
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: new string('a', 129),
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SblTransliteration);
    }

    [Fact]
    public void EmptyRootId_HasError()
    {
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: "ܟܬܒ",
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb,
            RootId: Guid.Empty);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RootId!.Value);
    }
}
