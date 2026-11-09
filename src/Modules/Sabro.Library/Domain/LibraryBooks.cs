namespace Sabro.Library.Domain;

/// <summary>
/// The books on the hub's Library shelf. A place or a mark names one of these plus an item key
/// (<see cref="LibraryItemKey"/>). The identifiers are the hub's address segments
/// (<c>/library/beth-gazo</c>…), never string literals at call sites.
/// </summary>
public static class LibraryBooks
{
    /// <summary>The words (the dictionary). Items are Lexicon entry ids.</summary>
    public const string Lexicon = "lexicon";

    /// <summary>The grammar book. Its pages live in the hub, so items are page slugs.</summary>
    public const string Grammar = "grammar";

    /// <summary>The treasury of chants. Items are chant ids.</summary>
    public const string BethGazo = "beth-gazo";

    /// <summary>The house of images (Shmo's figures). Items are historical figure ids.</summary>
    public const string BethSalme = "beth-salme";

    public static IReadOnlyList<string> All { get; } = [Lexicon, Grammar, BethGazo, BethSalme];

    public static bool IsKnown(string? book) => book is not null && All.Contains(book, StringComparer.Ordinal);

    /// <summary>True for the books whose items are rows in Sabro's database, keyed by a Guid.</summary>
    public static bool HasGuidItems(string book) => book is Lexicon or BethGazo or BethSalme;
}
