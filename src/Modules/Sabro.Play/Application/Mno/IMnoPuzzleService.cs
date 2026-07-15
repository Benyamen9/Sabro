using Sabro.Play.Domain;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Mno;

public interface IMnoPuzzleService
{
    Task<Result<MnoPuzzleDto>> GetTodaysPuzzleAsync(MnoDifficulty difficulty, CancellationToken cancellationToken);
}
