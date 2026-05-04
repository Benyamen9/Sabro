using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Authors;

public interface IAuthorService
{
    Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken);

    Task<Result<AuthorDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
