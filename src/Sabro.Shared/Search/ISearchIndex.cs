using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sabro.Shared.Search;

/// <summary>
/// Write-side abstraction over a search index. Implementations are best-effort:
/// failures are logged but do not throw, since the relational store is the
/// source of truth and a transient search outage must not break user writes.
/// Read-side query APIs are intentionally not part of this contract — search
/// endpoints are added later, on top of the same engine.
/// </summary>
/// <typeparam name="TDocument">The document type stored in the index.</typeparam>
public interface ISearchIndex<in TDocument>
    where TDocument : class
{
    Task UpsertAsync(TDocument document, CancellationToken cancellationToken);

    Task UpsertManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken);

    Task DeleteAsync(string documentId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes every document in the index and re-applies the descriptor's
    /// settings. Unlike <see cref="UpsertAsync"/> / <see cref="DeleteAsync"/>,
    /// this method PROPAGATES failures — it is invoked only by the admin
    /// rebuild path, where the operator must see errors rather than have
    /// them quietly swallowed.
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken);
}
