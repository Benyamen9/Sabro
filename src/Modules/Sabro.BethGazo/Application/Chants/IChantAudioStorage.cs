namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// Where a chant's recording is kept. Mirrors the Lexicon's pronunciation storage —
/// same media volume, same one-file-per-record shape.
/// </summary>
public interface IChantAudioStorage
{
    /// <summary>Writes the recording and returns the URL it is served from.</summary>
    Task<string> SaveAsync(Guid chantId, Stream content, string extension, CancellationToken cancellationToken);

    /// <summary>Deletes the file behind a URL. A no-op when it is already gone.</summary>
    void Delete(string url);
}
