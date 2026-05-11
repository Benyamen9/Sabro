using Sabro.Biblical.Domain;

namespace Sabro.UnitTests.Biblical.Domain;

public class BiblicalBookTests
{
    private const string MatthewSyriac = "ܡܬܝ";

    [Fact]
    public void Create_WithValidFields_ReturnsSuccess()
    {
        var result = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40, syriacName: MatthewSyriac);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("MAT");
        result.Value.EnglishName.Should().Be("Matthew");
        result.Value.SyriacName.Should().Be(MatthewSyriac);
        result.Value.Testament.Should().Be(Testament.New);
        result.Value.Order.Should().Be(40);
    }

    [Fact]
    public void Create_WithoutSyriacName_LeavesSyriacNameNull()
    {
        var result = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacName.Should().BeNull();
    }

    [Theory]
    [InlineData("mat")]
    [InlineData(" Mat ")]
    [InlineData("Mat")]
    public void Create_NormalizesCodeToUpperTrimmed(string code)
    {
        var result = BiblicalBook.Create(code, "Matthew", Testament.New, order: 40);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("MAT");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingCode_ReturnsValidationFailure(string? code)
    {
        var result = BiblicalBook.Create(code!, "Matthew", Testament.New, order: 40);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("M")]
    [InlineData("ABCDEFGHI")]
    [InlineData("MAT!")]
    [InlineData("MA T")]
    public void Create_WithInvalidCodeFormat_ReturnsValidationFailure(string code)
    {
        var result = BiblicalBook.Create(code, "Matthew", Testament.New, order: 40);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingEnglishName_ReturnsValidationFailure(string? name)
    {
        var result = BiblicalBook.Create("MAT", name!, Testament.New, order: 40);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithLatinSyriacName_ReturnsValidationFailure()
    {
        var result = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40, syriacName: "Matthew");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveOrder_ReturnsValidationFailure(int order)
    {
        var result = BiblicalBook.Create("MAT", "Matthew", Testament.New, order);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUndefinedTestament_ReturnsValidationFailure()
    {
        var result = BiblicalBook.Create("MAT", "Matthew", (Testament)999, order: 40);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_TrimsEnglishName()
    {
        var result = BiblicalBook.Create("MAT", "  Matthew  ", Testament.New, order: 40);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EnglishName.Should().Be("Matthew");
    }
}
