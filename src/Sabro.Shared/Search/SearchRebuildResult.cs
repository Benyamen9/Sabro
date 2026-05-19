using System;

namespace Sabro.Shared.Search;

/// <summary>
/// Outcome of a single index rebuild. Reports the number of documents
/// pushed to the engine and how long the operation took, so the operator
/// can verify the result without scraping logs.
/// </summary>
public sealed record SearchRebuildResult(int DocumentCount, TimeSpan Elapsed);
