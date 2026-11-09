using Sabro.Shared.Results;

namespace Sabro.Library.Domain;

/// <summary>
/// Normalizes the (book, item) pair a place or a mark points at, so the unique keys stay stable
/// however a client spells them: Guids in their lower-case <c>D</c> form, grammar slugs lower case.
/// </summary>
public static class LibraryItemKey
{
    public const int MaxSlugLength = 64;

    public static Result<string> NormalizeBook(string? book)
    {
        var trimmed = (book ?? string.Empty).Trim().ToLowerInvariant();
        return LibraryBooks.IsKnown(trimmed)
            ? Result<string>.Success(trimmed)
            : Result<string>.Failure(Field("book", $"Book must be one of: {string.Join(", ", LibraryBooks.All)}."));
    }

    /// <summary>
    /// Normalizes an item key for an already-normalized <paramref name="book"/>. The database books take
    /// a Guid; the grammar book takes a slug of lower-case letters, digits and hyphens.
    /// </summary>
    public static Result<string> Normalize(string book, string? itemKey)
    {
        var trimmed = (itemKey ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(Field("itemKey", "Item key is required."));
        }

        if (LibraryBooks.HasGuidItems(book))
        {
            return Guid.TryParse(trimmed, out var id) && id != Guid.Empty
                ? Result<string>.Success(id.ToString("D"))
                : Result<string>.Failure(Field("itemKey", "Item key must be an id for this book."));
        }

        var slug = trimmed.ToLowerInvariant();
        if (slug.Length > MaxSlugLength)
        {
            return Result<string>.Failure(Field("itemKey", $"Item key must be at most {MaxSlugLength} characters."));
        }

        foreach (var ch in slug)
        {
            if (ch is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '-'))
            {
                return Result<string>.Failure(Field("itemKey", "Item key may contain only lowercase letters, digits, and hyphens."));
            }
        }

        return Result<string>.Success(slug);
    }

    private static Error Field(string name, string message) =>
        Error.Validation(new Dictionary<string, IReadOnlyList<string>> { [name] = [message] });
}
