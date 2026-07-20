namespace Sabro.Lexicon.Application.Entries;

/// <summary>
/// Writes and removes pronunciation recordings under wwwroot/media (served as static files,
/// same locally-hosted approach as the documented bibliography-image storage — no S3 needed
/// at this scale). URLs returned/accepted are the site-relative path clients fetch, e.g.
/// "/media/pronunciations/{id}.mp3".
/// </summary>
public interface IPronunciationAudioStorage
{
    Task<string> SaveAsync(Guid entryId, Stream content, string extension, CancellationToken cancellationToken);

    void Delete(string url);
}
