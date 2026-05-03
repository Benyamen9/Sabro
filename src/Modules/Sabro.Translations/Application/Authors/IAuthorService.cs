using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Authors;

public interface IAuthorService
{
    Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken);
}
