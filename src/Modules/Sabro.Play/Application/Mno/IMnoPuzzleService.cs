using Sabro.Shared.Results;

namespace Sabro.Play.Application.Mno;

public interface IMnoPuzzleService
{
    Task<Result<MnoPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken);
}
