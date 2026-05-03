using Sabro.Shared.Domain;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.Translations.Domain;

public sealed class Author : Entity<Guid>, IAggregateRoot
{
    private Author(string name, string? syriacName, string? title)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Name = name;
        SyriacName = syriacName;
        Title = title;
    }

    public string Name { get; private set; }

    public string? SyriacName { get; private set; }

    public string? Title { get; private set; }

    public static Result<Author> Create(string name, string? syriacName = null, string? title = null)
    {
        var trimmedName = (name ?? string.Empty).Trim();
        if (trimmedName.Length == 0)
        {
            return Result<Author>.Failure(Error.Validation("Name is required."));
        }

        var trimmedSyriacName = string.IsNullOrWhiteSpace(syriacName) ? null : syriacName.Trim();
        if (trimmedSyriacName is not null)
        {
            var normalized = SyriacText.Normalize(trimmedSyriacName);
            if (!SyriacText.IsSyriacOnly(normalized))
            {
                return Result<Author>.Failure(Error.Validation("SyriacName must contain only Syriac script characters."));
            }

            trimmedSyriacName = normalized;
        }

        var trimmedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        return Result<Author>.Success(new Author(trimmedName, trimmedSyriacName, trimmedTitle));
    }
}
