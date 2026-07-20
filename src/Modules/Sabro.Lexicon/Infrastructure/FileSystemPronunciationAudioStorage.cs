using Microsoft.Extensions.Hosting;
using Sabro.Lexicon.Application.Entries;

namespace Sabro.Lexicon.Infrastructure;

internal sealed class FileSystemPronunciationAudioStorage : IPronunciationAudioStorage
{
    private readonly string mediaRoot;

    public FileSystemPronunciationAudioStorage(IHostEnvironment environment)
    {
        mediaRoot = Path.Combine(environment.ContentRootPath, "wwwroot", "media", "pronunciations");
    }

    public async Task<string> SaveAsync(Guid entryId, Stream content, string extension, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(mediaRoot);
        var fileName = $"{entryId}{extension}";
        var path = Path.Combine(mediaRoot, fileName);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"/media/pronunciations/{fileName}";
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
