using Microsoft.Extensions.Hosting;
using Sabro.BethGazo.Application.Chants;

namespace Sabro.BethGazo.Infrastructure;

/// <summary>
/// Stores recordings under <c>wwwroot/media/chants</c>, beside the Lexicon's
/// pronunciations and inside the same <c>media-data</c> volume.
/// </summary>
/// <remarks>
/// The directory ships in the image (a tracked <c>.gitkeep</c>), which is what gives
/// it uid 1001 ownership when the named volume initialises from it. A directory that
/// exists only at runtime under a volume mounted at a path absent from the image is
/// created <c>root:root</c>, and every write then fails with permission denied — the
/// exact failure the pronunciation volume was fixed for.
/// </remarks>
internal sealed class FileSystemChantAudioStorage : IChantAudioStorage
{
    private readonly string mediaRoot;

    public FileSystemChantAudioStorage(IHostEnvironment environment)
    {
        mediaRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "media", "chants");
    }

    public async Task<string> SaveAsync(Guid chantId, Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(mediaRoot);
        var fileName = $"{chantId}{extension}";
        var path = Path.Combine(mediaRoot, fileName);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"/media/chants/{fileName}";
    }

    public void Delete(string url)
    {
        var fileName = Path.GetFileName(url);
        if (fileName.Length == 0)
        {
            return;
        }

        var path = Path.Combine(mediaRoot, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
