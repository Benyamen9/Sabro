namespace Sabro.Shared.Search;

/// <summary>
/// Equality filter on a single indexed attribute. Multiple filters are
/// AND-joined by the underlying engine. Only fields declared as filterable
/// in the index descriptor may be filtered on. <see cref="Raw"/> emits
/// <see cref="Value"/> unquoted (e.g. for a boolean field, where Meilisearch
/// expects <c>field = true</c>, not <c>field = "true"</c>) instead of the
/// default double-quoted string comparison.
/// </summary>
public sealed record SearchFilter(string Field, string Value, bool Raw = false);
