using Sabro.BethGazo.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

public interface IChantService
{
    Task<Result<ChantDto>> CreateAsync(CreateChantRequest request, CancellationToken cancellationToken);

    Task<Result<ChantDto>> UpdateAsync(Guid id, UpdateChantRequest request, CancellationToken cancellationToken);

    Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<ChantDto>> PublishAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<ChantDto>> UnpublishAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<ChantDto>> SetPlayableAsync(Guid id, bool playable, CancellationToken cancellationToken);

    /// <summary>Attaches or clears the recording. Clearing is refused while published.</summary>
    Task<Result<ChantDto>> SetAudioAsync(Guid id, string? audioUrl, CancellationToken cancellationToken);

    /// <summary>Returns any chant regardless of status. For Owner/admin surfaces.</summary>
    Task<Result<ChantDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists chants of any status, newest first, with optional filters. For the
    /// backoffice. A direct relational query — the treasury is small enough that a
    /// search index would be plumbing without payoff.
    /// </summary>
    Task<Result<PagedResult<ChantDto>>> ListAsync(
        string? search,
        ChantStatus? status,
        Guid? modeId,
        bool? playableInNahlo,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>The modes, in traditional order, for the pickers.</summary>
    Task<IReadOnlyList<BethGazoModeDto>> ListModesAsync(CancellationToken cancellationToken);
}
