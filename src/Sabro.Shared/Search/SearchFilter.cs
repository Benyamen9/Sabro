namespace Sabro.Shared.Search;

/// <summary>
/// Equality filter on a single indexed attribute. Multiple filters are
/// AND-joined by the underlying engine. Only fields declared as filterable
/// in the index descriptor may be filtered on.
/// </summary>
public sealed record SearchFilter(string Field, string Value);
