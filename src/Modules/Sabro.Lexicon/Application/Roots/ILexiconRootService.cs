using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Roots;

public interface ILexiconRootService
{
    Task<Result<LexiconRootDto>> CreateAsync(CreateLexiconRootRequest request, CancellationToken cancellationToken);

    Task<Result<LexiconRootDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<LexiconRootDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
