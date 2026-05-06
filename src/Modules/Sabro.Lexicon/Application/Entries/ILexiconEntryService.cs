using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Entries;

public interface ILexiconEntryService
{
    Task<Result<LexiconEntryDto>> CreateAsync(CreateLexiconEntryRequest request, CancellationToken cancellationToken);

    Task<Result<LexiconEntryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResult<LexiconEntryDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
