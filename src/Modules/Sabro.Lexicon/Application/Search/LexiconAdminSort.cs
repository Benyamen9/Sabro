namespace Sabro.Lexicon.Application.Search;

/// <summary>
/// Ordering for the admin Lexicon backoffice list. Backed by the <c>lexicon</c>
/// Meilisearch index's sortable attributes, so it scales past the ~38,000-row
/// SEDRA import without an in-memory sort.
/// </summary>
public enum LexiconAdminSort
{
    /// <summary>Most recently created first (default) — the triage order for a fresh import batch.</summary>
    Recent = 0,

    /// <summary>Syriac alphabetical order of the unvocalized form.</summary>
    Syriac = 1,

    /// <summary>Groups by lifecycle status (Draft/Published).</summary>
    Status = 2,

    /// <summary>Shortest playable length first.</summary>
    Length = 3,
}
