using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Domain;

public class AuthorTests
{
    private const string DionysiosSyriac = "ܕܝܘܢܘܣܝܘܣ";

    [Fact]
    public void Create_WithValidName_ReturnsSuccessAndOptionalsAreNull()
    {
        var result = Author.Create(name: "Dionysios bar Salibi");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Dionysios bar Salibi");
        result.Value.SyriacName.Should().BeNull();
        result.Value.Title.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllFields_StoresAllFields()
    {
        var result = Author.Create(
            name: "Dionysios bar Salibi",
            syriacName: DionysiosSyriac,
            title: "Metropolitan of Amid");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Dionysios bar Salibi");
        result.Value.SyriacName.Should().Be(DionysiosSyriac);
        result.Value.Title.Should().Be("Metropolitan of Amid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingName_ReturnsValidationFailure(string? name)
    {
        var result = Author.Create(name: name!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsAllStringFields()
    {
        var result = Author.Create(
            name: "  Dionysios  ",
            syriacName: $"  {DionysiosSyriac}  ",
            title: "  Metropolitan  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Dionysios");
        result.Value.SyriacName.Should().Be(DionysiosSyriac);
        result.Value.Title.Should().Be("Metropolitan");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOptionalFields_StoresNull(string emptyValue)
    {
        var result = Author.Create(name: "Dionysios", syriacName: emptyValue, title: emptyValue);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacName.Should().BeNull();
        result.Value.Title.Should().BeNull();
    }

    [Fact]
    public void Create_WithLatinSyriacName_ReturnsValidationFailure()
    {
        var result = Author.Create(name: "Dionysios", syriacName: "Dionysios");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
