using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Sources;

public interface ISourceService
{
    Task<Result<SourceDto>> CreateAsync(CreateSourceRequest request, CancellationToken cancellationToken);

    Task<Result<SourceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<SourceDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
