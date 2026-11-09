using Sabro.Library.Domain;

namespace Sabro.UnitTests.Library.Domain;

public class LibraryItemKeyTests
{
    [Theory]
    [InlineData("lexicon")]
    [InlineData("grammar")]
    [InlineData("beth-gazo")]
    [InlineData("beth-salme")]
    public void NormalizeBook_AcceptsEveryBookOnTheShelf(string book)
    {
        LibraryItemKey.NormalizeBook(book).Value.Should().Be(book);
    }

    [Fact]
    public void NormalizeBook_IgnoresCaseAndSpaces()
    {
        LibraryItemKey.NormalizeBook("  Beth-Gazo ").Value.Should().Be(LibraryBooks.BethGazo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("library")]
    [InlineData("bethgazo")]
    public void NormalizeBook_RejectsAnythingElse(string? book)
    {
        var result = LibraryItemKey.NormalizeBook(book);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Fields.Should().ContainKey("book");
    }

    [Theory]
    [InlineData("lexicon")]
    [InlineData("beth-gazo")]
    [InlineData("beth-salme")]
    public void Normalize_GivesTheDatabaseBooksOneSpellingOfTheId(string book)
    {
        var id = Guid.NewGuid();

        LibraryItemKey.Normalize(book, id.ToString("B").ToUpperInvariant()).Value.Should().Be(id.ToString("D"));
    }

    [Theory]
    [InlineData("past")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Normalize_RefusesADatabaseBookItemThatIsNotAnId(string itemKey)
    {
        var result = LibraryItemKey.Normalize(LibraryBooks.Lexicon, itemKey);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Fields.Should().ContainKey("itemKey");
    }

    [Fact]
    public void Normalize_LowerCasesAGrammarSlug()
    {
        LibraryItemKey.Normalize(LibraryBooks.Grammar, " Past-Tense ").Value.Should().Be("past-tense");
    }

    [Theory]
    [InlineData("")]
    [InlineData("past tense")]
    [InlineData("past/tense")]
    [InlineData("ܡܠܬܐ")]
    public void Normalize_RefusesAGrammarSlugOutsideLettersDigitsAndHyphens(string itemKey)
    {
        LibraryItemKey.Normalize(LibraryBooks.Grammar, itemKey).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Normalize_RefusesAGrammarSlugThatIsTooLong()
    {
        var slug = new string('a', LibraryItemKey.MaxSlugLength + 1);

        LibraryItemKey.Normalize(LibraryBooks.Grammar, slug).IsSuccess.Should().BeFalse();
    }
}
