using Sabro.Biblical.Domain;

namespace Sabro.UnitTests.Biblical.Domain;

public class BiblicalPassageTests
{
    [Fact]
    public void Create_WithValidFields_ReturnsSuccess()
    {
        var bookId = Guid.NewGuid();

        var result = BiblicalPassage.Create(bookId, chapterNumber: 5, verseNumber: 3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BookId.Should().Be(bookId);
        result.Value.ChapterNumber.Should().Be(5);
        result.Value.VerseNumber.Should().Be(3);
    }

    [Fact]
    public void Create_WithEmptyBookId_ReturnsValidationFailure()
    {
        var result = BiblicalPassage.Create(Guid.Empty, chapterNumber: 1, verseNumber: 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveChapter_ReturnsValidationFailure(int chapter)
    {
        var result = BiblicalPassage.Create(Guid.NewGuid(), chapterNumber: chapter, verseNumber: 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveVerse_ReturnsValidationFailure(int verse)
    {
        var result = BiblicalPassage.Create(Guid.NewGuid(), chapterNumber: 1, verseNumber: verse);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
