using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Biblical.Domain;

public sealed class BiblicalPassage : Entity<Guid>, IAggregateRoot
{
    private BiblicalPassage(Guid bookId, int chapterNumber, int verseNumber)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        BookId = bookId;
        ChapterNumber = chapterNumber;
        VerseNumber = verseNumber;
    }

    public Guid BookId { get; private set; }

    public int ChapterNumber { get; private set; }

    public int VerseNumber { get; private set; }

    public static Result<BiblicalPassage> Create(Guid bookId, int chapterNumber, int verseNumber)
    {
        if (bookId == Guid.Empty)
        {
            return Result<BiblicalPassage>.Failure(Error.Validation("BookId is required."));
        }

        if (chapterNumber < 1)
        {
            return Result<BiblicalPassage>.Failure(Error.Validation("ChapterNumber must be 1 or greater."));
        }

        if (verseNumber < 1)
        {
            return Result<BiblicalPassage>.Failure(Error.Validation("VerseNumber must be 1 or greater."));
        }

        return Result<BiblicalPassage>.Success(new BiblicalPassage(bookId, chapterNumber, verseNumber));
    }
}
