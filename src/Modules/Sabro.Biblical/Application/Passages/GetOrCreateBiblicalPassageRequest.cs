namespace Sabro.Biblical.Application.Passages;

public sealed record GetOrCreateBiblicalPassageRequest(
    string BookCode,
    int ChapterNumber,
    int VerseNumber);
