using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Biblical.Application.Books;

public interface IBiblicalBookService
{
    Task<Result<BiblicalBookDto>> CreateAsync(CreateBiblicalBookRequest request, CancellationToken cancellationToken);

    Task<Result<BiblicalBookDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<BiblicalBookDto>> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Result<PagedResult<BiblicalBookDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
