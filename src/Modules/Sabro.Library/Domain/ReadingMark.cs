using Sabro.Shared.Domain;

namespace Sabro.Library.Domain;

/// <summary>
/// A ܀ mark: an item the reader marked to find again. One row per (user, book, item); marking twice
/// changes nothing, unmarking deletes the row.
/// </summary>
public sealed class ReadingMark : Entity<Guid>, IAggregateRoot
{
    /// <summary>A generous ceiling that no reader reaches by hand, so a script cannot fill the table.</summary>
    public const int MaxPerUser = 2000;

    private ReadingMark(string logtoUserId, string book, string itemKey, DateTimeOffset markedAt, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        CreatedAt = now;
        UpdatedAt = now;
        LogtoUserId = logtoUserId;
        Book = book;
        ItemKey = itemKey;
        MarkedAt = markedAt;
    }

    private ReadingMark()
    {
    }

    public string LogtoUserId { get; private set; } = string.Empty;

    /// <summary>One of <see cref="LibraryBooks"/>.</summary>
    public string Book { get; private set; } = string.Empty;

    /// <summary>The item, normalized by <see cref="LibraryItemKey"/>.</summary>
    public string ItemKey { get; private set; } = string.Empty;

    /// <summary>When the reader marked it, which orders the Marked panel (newest first).</summary>
    public DateTimeOffset MarkedAt { get; private set; }

    /// <summary>Inputs are already normalized and the user id trimmed; the service does both.</summary>
    internal static ReadingMark Create(string logtoUserId, string book, string itemKey, DateTimeOffset markedAt, DateTimeOffset now) =>
        new(logtoUserId, book, itemKey, markedAt, now);
}
