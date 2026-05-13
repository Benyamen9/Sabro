using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Annotations;

/// <summary>
/// Read-only cross-module surface for resolving an <c>Annotation</c> to its
/// parent (Source, Chapter, Verse) coordinates. Lives next to <see cref="IAnnotationService"/>
/// so callers consume the narrowest interface they need rather than the full
/// annotation CRUD surface.
/// </summary>
public interface IAnnotationLookupService
{
    Task<Result<AnnotationParentLocator>> GetParentLocatorAsync(Guid annotationId, CancellationToken cancellationToken);
}
