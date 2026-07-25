namespace Sabro.Shared.Search;

/// <summary>
/// Ordering instruction on a single sortable attribute. Multiple sorts apply
/// in list order (first is primary). Only fields declared as sortable in the
/// index descriptor may be sorted on.
/// </summary>
public sealed record SearchSort(string Field, bool Descending = false);
