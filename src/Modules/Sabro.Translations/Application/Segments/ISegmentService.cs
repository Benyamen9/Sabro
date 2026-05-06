using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Segments;

public interface ISegmentService
{
    Task<Result<SegmentDto>> CreateAsync(CreateSegmentRequest request, CancellationToken cancellationToken);

    Task<Result<SegmentDto>> EditAsync(EditSegmentRequest request, CancellationToken cancellationToken);

    Task<Result<SegmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<SegmentDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
