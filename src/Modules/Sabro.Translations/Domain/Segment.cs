using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Translations.Domain;

public sealed class Segment : Entity<Guid>, IAggregateRoot
{
    private Segment(Guid sourceId, int chapterNumber, int verseNumber, Guid textVersionId, string content)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        SourceId = sourceId;
        ChapterNumber = chapterNumber;
        VerseNumber = verseNumber;
        TextVersionId = textVersionId;
        Content = content;
    }

    public Guid SourceId { get; private set; }

    public int ChapterNumber { get; private set; }

    public int VerseNumber { get; private set; }

    public Guid TextVersionId { get; private set; }

    public string Content { get; private set; }

    public static Result<Segment> Create(
        Guid sourceId,
        int chapterNumber,
        int verseNumber,
        Guid textVersionId,
        string content)
    {
        if (sourceId == Guid.Empty)
        {
            return Result<Segment>.Failure(Error.Validation("SourceId is required."));
        }

        if (textVersionId == Guid.Empty)
        {
            return Result<Segment>.Failure(Error.Validation("TextVersionId is required."));
        }

        if (chapterNumber < 1)
        {
            return Result<Segment>.Failure(Error.Validation("ChapterNumber must be 1 or greater."));
        }

        if (verseNumber < 1)
        {
            return Result<Segment>.Failure(Error.Validation("VerseNumber must be 1 or greater."));
        }

        var trimmedContent = (content ?? string.Empty).Trim();
        if (trimmedContent.Length == 0)
        {
            return Result<Segment>.Failure(Error.Validation("Content is required."));
        }

        return Result<Segment>.Success(new Segment(sourceId, chapterNumber, verseNumber, textVersionId, trimmedContent));
    }
}
