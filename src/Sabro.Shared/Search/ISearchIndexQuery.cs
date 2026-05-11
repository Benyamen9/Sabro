using System.Threading;
using System.Threading.Tasks;
using Sabro.Shared.Pagination;

namespace Sabro.Shared.Search;

/// <summary>
/// Read-side abstraction over a search index. Unlike <see cref="ISearchIndex{TDocument}"/>,
/// implementations propagate failures — search reads are user-facing and should
/// surface a clear error (e.g. 503) rather than silently returning empty results.
/// </summary>
/// <typeparam name="TDocument">The document type stored in the index.</typeparam>
public interface ISearchIndexQuery<TDocument>
    where TDocument : class
{
    Task<PagedResult<TDocument>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
