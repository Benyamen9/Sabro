using Sabro.Shared.Results;

namespace Sabro.Library.Application.Reading;

/// <summary>The reader's places and marks in the hub's Library. Every call acts for one Logto user only.</summary>
public interface IReaderService
{
    Task<Result<ReaderShelf>> GetShelfAsync(string logtoUserId, CancellationToken cancellationToken);

    Task<Result<ReadingPlaceDto>> SetPlaceAsync(string logtoUserId, string book, SetReadingPlaceRequest request, CancellationToken cancellationToken);

    Task<Result<MarkItemOutcome>> MarkAsync(string logtoUserId, string book, string itemKey, MarkItemRequest? request, CancellationToken cancellationToken);

    /// <summary>Removes a mark. Unmarking something not marked succeeds, so a retried call is harmless.</summary>
    Task<Error?> UnmarkAsync(string logtoUserId, string book, string itemKey, CancellationToken cancellationToken);

    /// <summary>Deletes every place and mark of the user (account deletion). Returns how many rows went.</summary>
    Task<Result<int>> DeleteAllForUserAsync(string logtoUserId, CancellationToken cancellationToken);
}
