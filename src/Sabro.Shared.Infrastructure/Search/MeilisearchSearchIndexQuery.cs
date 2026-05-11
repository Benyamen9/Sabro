using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meilisearch;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;

namespace Sabro.Shared.Infrastructure.Search;

/// <summary>
/// Meilisearch implementation of <see cref="ISearchIndexQuery{TDocument}"/>.
/// Uses finite pagination (Page + HitsPerPage) so total hit counts are exact —
/// search endpoints expose <see cref="PagedResult{T}.Total"/> as a real total,
/// not Meilisearch's default estimated value. Failures propagate so callers
/// can surface a clear error response.
/// </summary>
/// <typeparam name="TDocument">The document type stored in the index.</typeparam>
internal sealed class MeilisearchSearchIndexQuery<TDocument> : ISearchIndexQuery<TDocument>
    where TDocument : class
{
    private readonly MeilisearchClient client;
    private readonly ISearchIndexDescriptor<TDocument> descriptor;
    private readonly ILogger<MeilisearchSearchIndexQuery<TDocument>> logger;

    public MeilisearchSearchIndexQuery(
        MeilisearchClient client,
        ISearchIndexDescriptor<TDocument> descriptor,
        ILogger<MeilisearchSearchIndexQuery<TDocument>> logger)
    {
        this.client = client;
        this.descriptor = descriptor;
        this.logger = logger;
    }

    public async Task<PagedResult<TDocument>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var index = client.Index(descriptor.IndexName);
        var meiliQuery = new Meilisearch.SearchQuery
        {
            Page = request.Page,
            HitsPerPage = request.PageSize,
            Filter = BuildFilter(request.Filters),
        };

        var response = await index.SearchAsync<TDocument>(request.Query ?? string.Empty, meiliQuery, cancellationToken)
            .ConfigureAwait(false);

        if (response is PaginatedSearchResult<TDocument> paginated)
        {
            logger.LogDebug(
                "Search dispatched. Index={IndexName} Query={Query} Page={Page} HitsPerPage={HitsPerPage} TotalHits={TotalHits}",
                descriptor.IndexName,
                request.Query,
                paginated.Page,
                paginated.HitsPerPage,
                paginated.TotalHits);

            return new PagedResult<TDocument>(
                paginated.Hits.ToArray(),
                paginated.TotalHits,
                paginated.Page,
                paginated.HitsPerPage);
        }

        var hits = response.Hits.ToArray();
        return new PagedResult<TDocument>(hits, hits.Length, request.Page, request.PageSize);
    }

    private static string? BuildFilter(IReadOnlyList<SearchFilter>? filters)
    {
        if (filters is null || filters.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < filters.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(" AND ");
            }

            var filter = filters[i];
            sb.Append(filter.Field);
            sb.Append(" = ");
            AppendQuoted(sb, filter.Value);
        }

        return sb.ToString();
    }

    private static void AppendQuoted(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var ch in value)
        {
            if (ch is '\\' or '"')
            {
                sb.Append('\\');
            }

            sb.Append(ch);
        }

        sb.Append('"');
    }
}
