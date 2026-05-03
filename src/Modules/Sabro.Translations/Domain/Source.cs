using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Translations.Domain;

public sealed class Source : Entity<Guid>, IAggregateRoot
{
    private Source(Guid authorId, string title, string? originalLanguageCode, string? description)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        AuthorId = authorId;
        Title = title;
        OriginalLanguageCode = originalLanguageCode;
        Description = description;
    }

    public Guid AuthorId { get; private set; }

    public string Title { get; private set; }

    public string? OriginalLanguageCode { get; private set; }

    public string? Description { get; private set; }

    public static Result<Source> Create(
        Guid authorId,
        string title,
        string? originalLanguageCode = null,
        string? description = null)
    {
        if (authorId == Guid.Empty)
        {
            return Result<Source>.Failure(Error.Validation("AuthorId is required."));
        }

        var trimmedTitle = (title ?? string.Empty).Trim();
        if (trimmedTitle.Length == 0)
        {
            return Result<Source>.Failure(Error.Validation("Title is required."));
        }

        var trimmedLanguageCode = string.IsNullOrWhiteSpace(originalLanguageCode)
            ? null
            : originalLanguageCode.Trim().ToLowerInvariant();

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        return Result<Source>.Success(new Source(authorId, trimmedTitle, trimmedLanguageCode, trimmedDescription));
    }
}
