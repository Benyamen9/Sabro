using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meilisearch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Search;

namespace Sabro.Shared.Infrastructure.Search;

/// <summary>
/// Ensures every registered <see cref="ISearchIndexDescriptor"/> has a matching
/// Meilisearch index with the expected primary key and settings (synonyms,
/// searchable attributes, filterable attributes). Runs once at startup.
/// Failures are logged but do not block host startup — the API remains usable
/// against Postgres even if Meilisearch is temporarily unavailable.
/// </summary>
internal sealed class SearchIndexInitializerHostedService : IHostedService
{
    private readonly MeilisearchClient client;
    private readonly ISearchIndexDescriptor[] descriptors;
    private readonly ILogger<SearchIndexInitializerHostedService> logger;

    public SearchIndexInitializerHostedService(
        MeilisearchClient client,
        IEnumerable<ISearchIndexDescriptor> descriptors,
        ILogger<SearchIndexInitializerHostedService> logger)
    {
        this.client = client;
        this.descriptors = descriptors.ToArray();
        this.logger = logger;
    }

    public static Settings ToMeilisearchSettings(IndexSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var meili = new Settings();

        if (source.SearchableAttributes.Count > 0)
        {
            meili.SearchableAttributes = source.SearchableAttributes.ToArray();
        }

        if (source.FilterableAttributes.Count > 0)
        {
            meili.FilterableAttributes = source.FilterableAttributes.ToArray();
        }

        if (source.Synonyms.Count > 0)
        {
            meili.Synonyms = source.Synonyms.ToDictionary(
                pair => pair.Key,
                pair => (IEnumerable<string>)pair.Value.ToArray());
        }

        return meili;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (descriptors.Length == 0)
        {
            logger.LogInformation("No search index descriptors registered — skipping Meilisearch initialization.");
            return;
        }

        foreach (var descriptor in descriptors)
        {
            await EnsureIndexAsync(descriptor, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Index initialization is best-effort at startup: a Meilisearch outage must not prevent the API from starting.")]
    private async Task EnsureIndexAsync(ISearchIndexDescriptor descriptor, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexExistsAsync(descriptor, cancellationToken).ConfigureAwait(false);
            await ApplySettingsAsync(descriptor, cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Meilisearch index initialized. Index={IndexName} PrimaryKey={PrimaryKey}",
                descriptor.IndexName,
                descriptor.PrimaryKey);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Meilisearch index initialization failed (best-effort, swallowed). Index={IndexName}",
                descriptor.IndexName);
        }
    }

    private async Task EnsureIndexExistsAsync(ISearchIndexDescriptor descriptor, CancellationToken cancellationToken)
    {
        try
        {
            await client.GetIndexAsync(descriptor.IndexName, cancellationToken).ConfigureAwait(false);
        }
        catch (MeilisearchApiError)
        {
            await client.CreateIndexAsync(descriptor.IndexName, descriptor.PrimaryKey, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ApplySettingsAsync(ISearchIndexDescriptor descriptor, CancellationToken cancellationToken)
    {
        var index = client.Index(descriptor.IndexName);
        var settings = ToMeilisearchSettings(descriptor.Settings);
        await index.UpdateSettingsAsync(settings, cancellationToken).ConfigureAwait(false);
    }
}
