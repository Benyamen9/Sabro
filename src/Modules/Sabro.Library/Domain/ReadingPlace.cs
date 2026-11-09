using Sabro.Shared.Domain;

namespace Sabro.Library.Domain;

/// <summary>
/// The red ribbon: where a reader last was in one book. One row per (user, book), moved every time
/// the reader opens another item there. Placed automatically, never by hand (Owner, 2026-11-01).
/// </summary>
public sealed class ReadingPlace : Entity<Guid>, IAggregateRoot
{
    private ReadingPlace(string logtoUserId, string book, string itemKey, DateTimeOffset openedAt, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        CreatedAt = now;
        UpdatedAt = now;
        LogtoUserId = logtoUserId;
        Book = book;
        ItemKey = itemKey;
        OpenedAt = openedAt;
    }

    private ReadingPlace()
    {
    }

    /// <summary>Opaque Logto user id (the OIDC <c>sub</c> claim) of the reader.</summary>
    public string LogtoUserId { get; private set; } = string.Empty;

    /// <summary>One of <see cref="LibraryBooks"/>.</summary>
    public string Book { get; private set; } = string.Empty;

    /// <summary>The item, normalized by <see cref="LibraryItemKey"/>.</summary>
    public string ItemKey { get; private set; } = string.Empty;

    /// <summary>When the reader opened the item: on another device, possibly before it reached Sabro.</summary>
    public DateTimeOffset OpenedAt { get; private set; }

    /// <summary>Inputs are already normalized and the user id trimmed; the service does both.</summary>
    internal static ReadingPlace Create(string logtoUserId, string book, string itemKey, DateTimeOffset openedAt, DateTimeOffset now) =>
        new(logtoUserId, book, itemKey, openedAt, now);

    /// <summary>
    /// Moves the ribbon, unless the place being sent was opened before the one already kept. A
    /// device that was signed out sends its place up later, and the most recently opened wins.
    /// </summary>
    /// <returns><c>true</c> when the ribbon moved.</returns>
    internal bool MoveTo(string itemKey, DateTimeOffset openedAt, DateTimeOffset now)
    {
        if (openedAt < OpenedAt)
        {
            return false;
        }

        ItemKey = itemKey;
        OpenedAt = openedAt;
        UpdatedAt = now;
        return true;
    }
}
