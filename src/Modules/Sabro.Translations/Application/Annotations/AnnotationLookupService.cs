using Microsoft.EntityFrameworkCore;
using Sabro.Shared.Results;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Annotations;

internal sealed class AnnotationLookupService : IAnnotationLookupService
{
    private readonly TranslationsDbContext dbContext;

    public AnnotationLookupService(TranslationsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Result<AnnotationParentLocator>> GetParentLocatorAsync(
        Guid annotationId,
        CancellationToken cancellationToken)
    {
        if (annotationId == Guid.Empty)
        {
            return Result<AnnotationParentLocator>.Failure(Error.Validation("AnnotationId is required."));
        }

        var locator = await (
            from a in dbContext.Annotations.AsNoTracking()
            join s in dbContext.Segments.AsNoTracking() on a.SegmentId equals s.Id
            where a.Id == annotationId
            select new AnnotationParentLocator(
                a.Id,
                a.Version,
                s.Id,
                s.SourceId,
                s.ChapterNumber,
                s.VerseNumber))
            .FirstOrDefaultAsync(cancellationToken);

        return locator is null
            ? Result<AnnotationParentLocator>.Failure(Error.NotFound($"Annotation {annotationId} not found."))
            : Result<AnnotationParentLocator>.Success(locator);
    }
}
