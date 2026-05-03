using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Domain;

public class SourceTests
{
    private static readonly Guid AnAuthorId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidAuthorIdAndTitle_ReturnsSuccessAndOptionalsAreNull()
    {
        var result = Source.Create(authorId: AnAuthorId, title: "Commentary on Matthew");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AuthorId.Should().Be(AnAuthorId);
        result.Value.Title.Should().Be("Commentary on Matthew");
        result.Value.OriginalLanguageCode.Should().BeNull();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllFields_StoresAllFields()
    {
        var result = Source.Create(
            authorId: AnAuthorId,
            title: "Commentary on Matthew",
            originalLanguageCode: "syr",
            description: "12th century commentary by bar Salibi.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginalLanguageCode.Should().Be("syr");
        result.Value.Description.Should().Be("12th century commentary by bar Salibi.");
    }

    [Fact]
    public void Create_WithEmptyAuthorId_ReturnsValidationFailure()
    {
        var result = Source.Create(authorId: Guid.Empty, title: "Commentary on Matthew");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingTitle_ReturnsValidationFailure(string? title)
    {
        var result = Source.Create(authorId: AnAuthorId, title: title!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsAllStringFields()
    {
        var result = Source.Create(
            authorId: AnAuthorId,
            title: "  Commentary on Matthew  ",
            originalLanguageCode: "  SYR  ",
            description: "  Some description.  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Commentary on Matthew");
        result.Value.OriginalLanguageCode.Should().Be("syr");
        result.Value.Description.Should().Be("Some description.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOptionalFields_StoresNull(string emptyValue)
    {
        var result = Source.Create(
            authorId: AnAuthorId,
            title: "Commentary on Matthew",
            originalLanguageCode: emptyValue,
            description: emptyValue);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OriginalLanguageCode.Should().BeNull();
        result.Value.Description.Should().BeNull();
    }
}
