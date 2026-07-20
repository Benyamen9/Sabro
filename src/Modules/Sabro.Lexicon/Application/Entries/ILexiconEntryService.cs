using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Entries;

public interface ILexiconEntryService
{
    Task<Result<LexiconEntryDto>> CreateAsync(CreateLexiconEntryRequest request, CancellationToken cancellationToken);

    Task<Result<LexiconEntryDto>> UpdateAsync(Guid id, UpdateLexiconEntryRequest request, CancellationToken cancellationToken);

    Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<LexiconEntryDto>> PublishAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<LexiconEntryDto>> UnpublishAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<LexiconEntryDto>> SetPlayableAsync(Guid id, bool playable, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the uploaded stream as the entry's pronunciation recording, replacing (and deleting)
    /// any previous one. <paramref name="extension"/> is the validated file extension including the
    /// leading dot (e.g. ".mp3"), chosen by the caller from the request's content type.
    /// </summary>
    Task<Result<LexiconEntryDto>> UploadPronunciationAudioAsync(
        Guid id, Stream content, string extension, CancellationToken cancellationToken);

    /// <summary>Deletes the entry's pronunciation recording, if any. Idempotent.</summary>
    Task<Result<LexiconEntryDto>> RemovePronunciationAudioAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns any entry regardless of status. For Owner/admin surfaces.</summary>
    Task<Result<LexiconEntryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns an entry only when it is published. For public/client surfaces.</summary>
    Task<Result<LexiconEntryDto>> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists entries of any status. For Owner/admin surfaces.</summary>
    Task<Result<PagedResult<LexiconEntryDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Lists only published entries. For public/client surfaces.</summary>
    Task<Result<PagedResult<LexiconEntryDto>>> ListPublishedAsync(int page, int pageSize, CancellationToken cancellationToken);
}
