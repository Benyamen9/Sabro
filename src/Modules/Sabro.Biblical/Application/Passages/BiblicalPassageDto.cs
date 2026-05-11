namespace Sabro.Biblical.Application.Passages;

public sealed record BiblicalPassageDto(
    Guid Id,
    Guid BookId,
    string BookCode,
    int ChapterNumber,
    int VerseNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
