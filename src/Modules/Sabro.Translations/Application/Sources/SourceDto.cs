namespace Sabro.Translations.Application.Sources;

public sealed record SourceDto(
    Guid Id,
    Guid AuthorId,
    string Title,
    string? OriginalLanguageCode,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
