using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Annotations;

public interface IAnnotationService
{
    Task<Result<AnnotationDto>> CreateAsync(CreateAnnotationRequest request, CancellationToken cancellationToken);

    Task<Result<AnnotationDto>> EditAsync(EditAnnotationRequest request, CancellationToken cancellationToken);

    Task<Result<AnnotationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<AnnotationDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
