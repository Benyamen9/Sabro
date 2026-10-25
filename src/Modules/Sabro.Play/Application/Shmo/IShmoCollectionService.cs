using Sabro.Shared.Results;

namespace Sabro.Play.Application.Shmo;

public interface IShmoCollectionService
{
    /// <summary>The figures the player has met or found in Shmo, from their recorded results.</summary>
    Task<Result<ShmoCollectionDto>> GetForUserAsync(string logtoUserId, CancellationToken cancellationToken);
}
