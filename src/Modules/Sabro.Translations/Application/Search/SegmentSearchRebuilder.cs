using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Search;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Search;

internal sealed class SegmentSearchRebuilder : ISearchRebuilder
{
    private const int BatchSize = 200;

    private readonly TranslationsDbContext dbContext;
    private readonly ISearchIndex<SegmentSearchDocument> searchIndex;
    private readonly ISearchIndexDescriptor<SegmentSearchDocument> descriptor;
    private readonly ILogger<SegmentSearchRebuilder> logger;

    public SegmentSearchRebuilder(
        TranslationsDbContext dbContext,
        ISearchIndex<SegmentSearchDocument> searchIndex,
        ISearchIndexDescriptor<SegmentSearchDocument> descriptor,
        ILogger<SegmentSearchRebuilder> logger)
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

        var latest = await dbContext.Segments
            .AsNoTracking()
            .Where(s => !dbContext.Segments.Any(s2 => s2.PreviousVersionId == s.Id))
            .ToListAsync(cancellationToken);

        var indexed = 0;
        var batch = new List<SegmentSearchDocument>(BatchSize);
        foreach (var segment in latest)
        {
            batch.Add(SegmentDocumentMapper.Map(segment));

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
            "Segment search index rebuilt. Index={IndexName} DocumentCount={Count} ElapsedMs={Elapsed}",
            descriptor.IndexName,
            indexed,
            stopwatch.Elapsed.TotalMilliseconds);

        return new SearchRebuildResult(indexed, stopwatch.Elapsed);
    }
}
