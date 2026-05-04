using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Sources;

public interface ISourceService
{
    Task<Result<SourceDto>> CreateAsync(CreateSourceRequest request, CancellationToken cancellationToken);
}
