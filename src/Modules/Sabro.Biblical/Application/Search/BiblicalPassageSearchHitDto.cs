using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Application.Search;

/// <summary>
/// Public projection of a biblical passage search hit. Carries the
/// denormalized book metadata so callers can render a result row without
/// a follow-up fetch against the relational store.
/// </summary>
public sealed record BiblicalPassageSearchHitDto(
    Guid Id,
    Guid BookId,
    string BookCode,
    string BookEnglishName,
    string? BookSyriacName,
    Testament Testament,
    int BookOrder,
    int ChapterNumber,
    int VerseNumber,
    string Reference);
