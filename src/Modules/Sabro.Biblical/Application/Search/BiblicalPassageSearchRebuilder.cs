using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Biblical.Infrastructure;
using Sabro.Shared.Search;

namespace Sabro.Biblical.Application.Search;

internal sealed class BiblicalPassageSearchRebuilder : ISearchRebuilder
{
    private const int BatchSize = 200;

    private readonly BiblicalDbContext dbContext;
    private readonly ISearchIndex<BiblicalPassageSearchDocument> searchIndex;
    private readonly ISearchIndexDescriptor<BiblicalPassageSearchDocument> descriptor;
    private readonly ILogger<BiblicalPassageSearchRebuilder> logger;

    public BiblicalPassageSearchRebuilder(
        BiblicalDbContext dbContext,
        ISearchIndex<BiblicalPassageSearchDocument> searchIndex,
        ISearchIndexDescriptor<BiblicalPassageSearchDocument> descriptor,
        ILogger<BiblicalPassageSearchRebuilder> logger)
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

        var rows = await (from p in dbContext.Passages.AsNoTracking()
                          join b in dbContext.Books.AsNoTracking() on p.BookId equals b.Id
                          select new { Passage = p, Book = b })
            .ToListAsync(cancellationToken);

        var indexed = 0;
        var batch = new List<BiblicalPassageSearchDocument>(BatchSize);
        foreach (var row in rows)
        {
            batch.Add(BiblicalPassageDocumentMapper.Map(row.Passage, row.Book));

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
            "BiblicalPassage search index rebuilt. Index={IndexName} DocumentCount={Count} ElapsedMs={Elapsed}",
            descriptor.IndexName,
            indexed,
            stopwatch.Elapsed.TotalMilliseconds);

        return new SearchRebuildResult(indexed, stopwatch.Elapsed);
    }
}
