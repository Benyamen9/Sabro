using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Search;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Search;

/// <summary>
/// Rebuilds the <c>annotations</c> search index from PostgreSQL. The
/// approvalStatus field is left null on every emitted document — Reviews
/// owns approval verdicts and pushes them via the existing
/// <c>IAnnotationApprovalIndexer</c> surface. The operator runs the Reviews
/// republisher after this rebuilder to refill approval statuses.
/// </summary>
internal sealed class AnnotationSearchRebuilder : ISearchRebuilder
{
    private const int BatchSize = 200;

    private readonly TranslationsDbContext dbContext;
    private readonly ISearchIndex<AnnotationSearchDocument> searchIndex;
    private readonly ISearchIndexDescriptor<AnnotationSearchDocument> descriptor;
    private readonly ILogger<AnnotationSearchRebuilder> logger;

    public AnnotationSearchRebuilder(
        TranslationsDbContext dbContext,
        ISearchIndex<AnnotationSearchDocument> searchIndex,
        ISearchIndexDescriptor<AnnotationSearchDocument> descriptor,
        ILogger<AnnotationSearchRebuilder> logger)
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

        var rows = await (from a in dbContext.Annotations.AsNoTracking()
                          where !dbContext.Annotations.Any(a2 => a2.PreviousVersionId == a.Id)
                          join s in dbContext.Segments.AsNoTracking() on a.SegmentId equals s.Id
                          select new { Annotation = a, Segment = s })
            .ToListAsync(cancellationToken);

        var indexed = 0;
        var batch = new List<AnnotationSearchDocument>(BatchSize);
        foreach (var row in rows)
        {
            batch.Add(AnnotationDocumentMapper.Map(row.Annotation, row.Segment));

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
            "Annotation search index rebuilt. Index={IndexName} DocumentCount={Count} ElapsedMs={Elapsed}",
            descriptor.IndexName,
            indexed,
            stopwatch.Elapsed.TotalMilliseconds);

        return new SearchRebuildResult(indexed, stopwatch.Elapsed);
    }
}
