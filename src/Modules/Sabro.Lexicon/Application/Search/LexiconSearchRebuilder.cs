using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Search;

namespace Sabro.Lexicon.Application.Search;

internal sealed class LexiconSearchRebuilder : ISearchRebuilder
{
    private const int BatchSize = 200;

    private readonly LexiconDbContext dbContext;
    private readonly ISearchIndex<LexiconEntrySearchDocument> searchIndex;
    private readonly ISearchIndexDescriptor<LexiconEntrySearchDocument> descriptor;
    private readonly ILogger<LexiconSearchRebuilder> logger;

    public LexiconSearchRebuilder(
        LexiconDbContext dbContext,
        ISearchIndex<LexiconEntrySearchDocument> searchIndex,
        ISearchIndexDescriptor<LexiconEntrySearchDocument> descriptor,
        ILogger<LexiconSearchRebuilder> logger)
    {
        this.dbContext = dbContext;
        this.searchIndex = searchIndex;
        this.descriptor = descriptor;
        this.logger = logger;
    }

    public string IndexName => descriptor.IndexName;

    public async Task<SearchRebuildResult> RebuildAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await searchIndex.ResetAsync(cancellationToken);

        var rootForms = await dbContext.Roots
            .AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Form, cancellationToken);

        var entries = await dbContext.Entries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var indexed = 0;
        var batch = new List<LexiconEntrySearchDocument>(BatchSize);
        foreach (var entry in entries)
        {
            string? rootForm = null;
            if (entry.RootId is { } rid && rootForms.TryGetValue(rid, out var form))
            {
                rootForm = form;
            }

            batch.Add(LexiconEntryDocumentMapper.Map(entry, rootForm));

            if (batch.Count >= BatchSize)
            {
                await searchIndex.UpsertManyAsync(batch, cancellationToken);
                indexed += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await searchIndex.UpsertManyAsync(batch, cancellationToken);
            indexed += batch.Count;
        }

        stopwatch.Stop();

        logger.LogInformation(
            "Lexicon search index rebuilt. Index={IndexName} DocumentCount={Count} ElapsedMs={Elapsed}",
            descriptor.IndexName,
            indexed,
            stopwatch.Elapsed.TotalMilliseconds);

        return new SearchRebuildResult(indexed, stopwatch.Elapsed);
    }
}
