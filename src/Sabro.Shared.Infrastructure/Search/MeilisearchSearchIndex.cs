using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meilisearch;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Search;

namespace Sabro.Shared.Infrastructure.Search;

/// <summary>
/// Meilisearch implementation of <see cref="ISearchIndex{TDocument}"/>.
/// All write operations are best-effort: any failure is logged with full
/// context but never propagated, because the relational store is the source
/// of truth and a search outage must not fail user writes.
/// </summary>
/// <typeparam name="TDocument">The document type stored in the index.</typeparam>
internal sealed class MeilisearchSearchIndex<TDocument> : ISearchIndex<TDocument>
    where TDocument : class
{
    private readonly MeilisearchClient client;
    private readonly ISearchIndexDescriptor<TDocument> descriptor;
    private readonly ILogger<MeilisearchSearchIndex<TDocument>> logger;

    public MeilisearchSearchIndex(
        MeilisearchClient client,
        ISearchIndexDescriptor<TDocument> descriptor,
        ILogger<MeilisearchSearchIndex<TDocument>> logger)
    {
        this.client = client;
        this.descriptor = descriptor;
        this.logger = logger;
    }

    public Task UpsertAsync(TDocument document, CancellationToken cancellationToken) =>
        UpsertManyAsync(new[] { document }, cancellationToken);

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Best-effort search sync: any failure must be logged but not propagated. Postgres is the source of truth.")]
    public async Task UpsertManyAsync(IEnumerable<TDocument> documents, CancellationToken cancellationToken)
    {
        var batch = documents as IReadOnlyCollection<TDocument> ?? documents.ToArray();
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            var index = client.Index(descriptor.IndexName);
            await index.AddDocumentsAsync(batch, descriptor.PrimaryKey, cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "Search upsert dispatched. Index={IndexName} DocumentType={DocumentType} Count={Count}",
                descriptor.IndexName,
                typeof(TDocument).Name,
                batch.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Search upsert failed (best-effort, swallowed). Index={IndexName} DocumentType={DocumentType} Count={Count}",
                descriptor.IndexName,
                typeof(TDocument).Name,
                batch.Count);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Best-effort search sync: any failure must be logged but not propagated. Postgres is the source of truth.")]
    public async Task DeleteAsync(string documentId, CancellationToken cancellationToken)
    {
        try
        {
            var index = client.Index(descriptor.IndexName);
            await index.DeleteOneDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);

            logger.LogDebug(
                "Search delete dispatched. Index={IndexName} DocumentId={DocumentId}",
                descriptor.IndexName,
                documentId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Search delete failed (best-effort, swallowed). Index={IndexName} DocumentId={DocumentId}",
                descriptor.IndexName,
                documentId);
        }
    }
}
