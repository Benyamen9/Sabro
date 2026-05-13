using System.Globalization;
using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Application.Search;

internal static class BiblicalPassageDocumentMapper
{
    public static BiblicalPassageSearchDocument Map(BiblicalPassage passage, BiblicalBook book) => new()
    {
        Id = passage.Id.ToString("D"),
        BookId = book.Id.ToString("D"),
        BookCode = book.Code,
        BookEnglishName = book.EnglishName,
        BookSyriacName = book.SyriacName,
        Testament = book.Testament.ToString(),
        BookOrder = book.Order,
        ChapterNumber = passage.ChapterNumber,
        VerseNumber = passage.VerseNumber,
        Reference = string.Create(
            CultureInfo.InvariantCulture,
            $"{book.EnglishName} {passage.ChapterNumber}:{passage.VerseNumber}"),
    };
}
