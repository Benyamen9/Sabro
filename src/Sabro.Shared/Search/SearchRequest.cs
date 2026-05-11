using System.Collections.Generic;

namespace Sabro.Shared.Search;

/// <summary>
/// Engine-agnostic search request. <see cref="Query"/> is the user's free-text
/// term (matched against the index's searchable attributes); <see cref="Filters"/>
/// narrow the candidate set by equality on filterable attributes. Pagination
/// is finite — totals are exact, not estimated.
/// </summary>
public sealed record SearchRequest(
    string? Query,
    int Page = 1,
    int PageSize = 50,
    IReadOnlyList<SearchFilter>? Filters = null);
