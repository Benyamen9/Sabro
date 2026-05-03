using FluentValidation.TestHelper;
using Sabro.Translations.Application.Authors;

namespace Sabro.UnitTests.Translations.Application;

public class CreateAuthorRequestValidatorTests
{
    private readonly CreateAuthorRequestValidator validator = new();

    [Fact]
    public void ValidInput_HasNoErrors()
    {
        var request = new CreateAuthorRequest(Name: "Dionysios", SyriacName: null, Title: null);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MissingName_HasErrorOnName(string? name)
    {
        var request = new CreateAuthorRequest(Name: name!, SyriacName: null, Title: null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameLongerThan256_HasErrorOnName()
    {
        var request = new CreateAuthorRequest(Name: new string('a', 257), SyriacName: null, Title: null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void SyriacNameLongerThan256_HasErrorOnSyriacName()
    {
        var request = new CreateAuthorRequest(Name: "Author", SyriacName: new string('a', 257), Title: null);

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.SyriacName);
    }

    [Fact]
    public void TitleLongerThan256_HasErrorOnTitle()
    {
        var request = new CreateAuthorRequest(Name: "Author", SyriacName: null, Title: new string('a', 257));

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void NullableOptionalsAreAllowed()
    {
        var request = new CreateAuthorRequest(Name: "Author", SyriacName: null, Title: null);

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SyriacName);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }
}
