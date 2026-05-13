using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;

namespace Sabro.UnitTests.Biblical.Application.Search;

public class BiblicalPassageDocumentMapperTests
{
    [Fact]
    public void Map_PopulatesAllFieldsAndDerivesReference()
    {
        var book = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40, syriacName: "ܡܬܝ").Value!;
        var passage = BiblicalPassage.Create(book.Id, chapterNumber: 3, verseNumber: 7).Value!;

        var doc = BiblicalPassageDocumentMapper.Map(passage, book);

        doc.Id.Should().Be(passage.Id.ToString("D"));
        doc.BookId.Should().Be(book.Id.ToString("D"));
        doc.BookCode.Should().Be("MAT");
        doc.BookEnglishName.Should().Be("Matthew");
        doc.BookSyriacName.Should().Be("ܡܬܝ");
        doc.Testament.Should().Be("New");
        doc.BookOrder.Should().Be(40);
        doc.ChapterNumber.Should().Be(3);
        doc.VerseNumber.Should().Be(7);
        doc.Reference.Should().Be("Matthew 3:7");
    }

    [Fact]
    public void Map_WithNullSyriacName_CarriesNullThrough()
    {
        var book = BiblicalBook.Create("GEN", "Genesis", Testament.Old, order: 1).Value!;
        var passage = BiblicalPassage.Create(book.Id, 1, 1).Value!;

        var doc = BiblicalPassageDocumentMapper.Map(passage, book);

        doc.BookSyriacName.Should().BeNull();
        doc.Reference.Should().Be("Genesis 1:1");
    }
}
