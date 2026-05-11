using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Application.Books;

public sealed record BiblicalBookDto(
    Guid Id,
    string Code,
    string EnglishName,
    string? SyriacName,
    Testament Testament,
    int Order,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
