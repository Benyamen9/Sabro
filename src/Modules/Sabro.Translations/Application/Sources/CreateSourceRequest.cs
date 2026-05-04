namespace Sabro.Translations.Application.Sources;

public sealed record CreateSourceRequest(
    Guid AuthorId,
    string Title,
    string? OriginalLanguageCode,
    string? Description);
