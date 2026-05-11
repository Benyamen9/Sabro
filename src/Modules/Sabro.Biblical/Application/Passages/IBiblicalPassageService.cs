using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Biblical.Application.Passages;

public interface IBiblicalPassageService
{
    Task<Result<BiblicalPassageLookupResult>> GetOrCreateAsync(GetOrCreateBiblicalPassageRequest request, CancellationToken cancellationToken);

    Task<Result<BiblicalPassageDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<BiblicalPassageDto>>> ListAsync(string? bookCode, int? chapterNumber, int page, int pageSize, CancellationToken cancellationToken);
}
