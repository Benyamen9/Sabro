using Microsoft.AspNetCore.StaticFiles;

namespace Sabro.API.Media;

/// <summary>
/// The audio formats accepted for pronunciation recordings, declared once for both
/// directions: which uploads are allowed in, and which content type each is served
/// back out with.
/// </summary>
/// <remarks>
/// <para>
/// These two halves used to live apart — the accepted list on the admin controller,
/// the served type inherited from <see cref="FileExtensionContentTypeProvider"/>'s
/// defaults — and they disagreed. That provider is derived from IIS's table, which
/// predates the audio-only uses of these containers: <c>.ogg</c> maps to
/// <c>video/ogg</c> and <c>.webm</c> to <c>video/webm</c>. So the API would accept a
/// recording as <c>audio/webm</c>, store it, and then serve it back labelled as
/// video, which an <c>&lt;audio&gt;</c> element is entitled to refuse.
/// </para>
/// <para>
/// Keeping one table means a format cannot be accepted without also declaring how it
/// is served.
/// </para>
/// </remarks>
public static class PronunciationAudioFormats
{
    /// <summary>
    /// Upload content types mapped to the extension the recording is stored under.
    /// Several spellings are accepted per format because browsers disagree (Safari
    /// sends <c>audio/x-m4a</c>, Chrome <c>audio/mp4</c>).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ExtensionsByUploadContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["audio/mpeg"] = ".mp3",
            ["audio/mp3"] = ".mp3",
            ["audio/wav"] = ".wav",
            ["audio/x-wav"] = ".wav",
            ["audio/ogg"] = ".ogg",
            ["audio/webm"] = ".webm",
            ["audio/mp4"] = ".m4a",
            ["audio/x-m4a"] = ".m4a",
        };

    /// <summary>
    /// The content type each stored extension is served with. Every value is an
    /// <c>audio/*</c> type — these files are always recordings, never video.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ServedContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".ogg"] = "audio/ogg",
            [".webm"] = "audio/webm",
            [".m4a"] = "audio/mp4",
        };

    /// <summary>
    /// Builds the static-file content type provider for <c>wwwroot</c>, starting from
    /// the framework defaults and overriding the audio extensions above. Extensions
    /// with no mapping stay unserved, which is the default and deliberate: it is what
    /// stops an arbitrary file dropped into the media directory from being handed out.
    /// </summary>
    public static FileExtensionContentTypeProvider CreateContentTypeProvider()
    {
        var provider = new FileExtensionContentTypeProvider();
        foreach (var (extension, contentType) in ServedContentTypesByExtension)
        {
            provider.Mappings[extension] = contentType;
        }

        return provider;
    }
}
