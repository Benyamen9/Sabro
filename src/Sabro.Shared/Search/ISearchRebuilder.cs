using System.Threading;
using System.Threading.Tasks;

namespace Sabro.Shared.Search;

/// <summary>
/// Admin operation that wipes a search index and rebuilds it from PostgreSQL.
/// Per CLAUDE.md, Meilisearch indexes are not backed up — they are rebuilt on
/// demand from the relational source of truth. Modules register one rebuilder
/// per index; the admin endpoint dispatches by <see cref="IndexName"/>.
/// Failures propagate so the operator sees them — unlike best-effort upsert.
/// </summary>
public interface ISearchRebuilder
{
    /// <summary>
    /// Index name the rebuilder targets. Must match the
    /// <see cref="ISearchIndexDescriptor.IndexName"/> of the rebuilder's index.
    /// </summary>
    string IndexName { get; }

    Task<SearchRebuildResult> RebuildAsync(CancellationToken cancellationToken);
}
