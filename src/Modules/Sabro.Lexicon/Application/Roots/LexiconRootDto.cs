namespace Sabro.Lexicon.Application.Roots;

public sealed record LexiconRootDto(
    Guid Id,
    string Form,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
