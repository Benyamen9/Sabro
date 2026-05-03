namespace Sabro.Translations.Application.Authors;

public sealed record AuthorDto(
    Guid Id,
    string Name,
    string? SyriacName,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
