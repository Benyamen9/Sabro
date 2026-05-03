namespace Sabro.Translations.Application.Authors;

public sealed record CreateAuthorRequest(
    string Name,
    string? SyriacName,
    string? Title);
