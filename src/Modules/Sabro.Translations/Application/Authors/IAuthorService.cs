using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Authors;

public interface IAuthorService
{
    Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken);

    Task<Result<AuthorDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<AuthorDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
