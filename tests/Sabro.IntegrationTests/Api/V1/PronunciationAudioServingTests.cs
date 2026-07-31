using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sabro.API.Media;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Pronunciation recordings end to end: uploaded through the admin endpoint, then
/// fetched back over the static-file pipeline the browser actually uses.
/// </summary>
/// <remarks>
/// Written after production served a recording uploaded as <c>audio/webm</c> back as
/// <c>video/webm</c>, because the framework's default content type table is derived
/// from IIS's and predates the audio-only use of that container. Nothing failed
/// loudly — the file stored fine and returned 200; an <c>&lt;audio&gt;</c> element is
/// simply entitled to refuse a video type. Asserting the stored bytes alone would
/// have missed it, so these tests assert the served <c>Content-Type</c>.
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class PronunciationAudioServingTests : IDisposable
{
    private const string Unvocalized = "ܩܠܐ";

    private readonly SabroApiFactory factory;
    private readonly HttpClient client;
    private readonly List<string> uploadedUrls = new();

    public PronunciationAudioServingTests(PostgresFixture postgres)
    {
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    public static TheoryData<string, string> AcceptedFormats() => new()
    {
        { "audio/mpeg", ".mp3" },
        { "audio/wav", ".wav" },
        { "audio/ogg", ".ogg" },
        { "audio/webm", ".webm" },
        { "audio/mp4", ".m4a" },
        { "audio/x-m4a", ".m4a" },
    };

    [Theory]
    [MemberData(nameof(AcceptedFormats))]
    public async Task UploadedRecording_IsServedBackAsAudio(string uploadContentType, string expectedExtension)
    {
        var ct = TestContext.Current.CancellationToken;
        var entryId = await CreateEntryAsync(ct);

        var url = await UploadAsync(entryId, uploadContentType, ct);
        url.Should().EndWith(expectedExtension);

        var fetched = await client.GetAsync(url, ct);

        // A 404 here means the extension has no content type mapping at all: the file
        // is on disk and in the database, and silently unplayable.
        fetched.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"a recording uploaded as {uploadContentType} must be downloadable again");

        fetched.Content.Headers.ContentType!.MediaType.Should().StartWith(
            "audio/",
            $"{expectedExtension} holds a voice recording, so it must not be served as video/*");
    }

    [Fact]
    public async Task UploadedRecording_KeepsItsBytes()
    {
        var ct = TestContext.Current.CancellationToken;
        var entryId = await CreateEntryAsync(ct);
        var url = await UploadAsync(entryId, "audio/mpeg", ct);

        var fetched = await client.GetAsync(url, ct);
        var bytes = await fetched.Content.ReadAsByteArrayAsync(ct);

        bytes.Should().Equal(SampleRecording(), "the stored file must round-trip unmodified");
    }

    [Fact]
    public void EveryAcceptedUploadFormat_HasAServedContentType()
    {
        // The two halves are declared together precisely so they cannot drift; this
        // fails if a format is ever accepted for upload with no way to serve it back.
        foreach (var extension in PronunciationAudioFormats.ExtensionsByUploadContentType.Values.Distinct())
        {
            PronunciationAudioFormats.ServedContentTypesByExtension
                .Should().ContainKey(extension);
            PronunciationAudioFormats.ServedContentTypesByExtension[extension]
                .Should().StartWith("audio/");
        }
    }

    public void Dispose()
    {
        // Recordings are written into the API project's wwwroot, which is shared with
        // the developer's own files — leave nothing behind.
        if (uploadedUrls.Count > 0)
        {
            var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
            foreach (var url in uploadedUrls)
            {
                var path = Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot",
                    url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    // Deliberately not a real audio stream: the endpoint validates the declared
    // content type, not the container, and these tests are about transport.
    private static byte[] SampleRecording() =>
        new byte[] { 0x53, 0x41, 0x42, 0x52, 0x4F, 0x00, 0x01, 0x02, 0x03, 0x04 };

    private async Task<Guid> CreateEntryAsync(CancellationToken ct)
    {
        var body = new CreateLexiconEntryRequest(
            SyriacUnvocalized: Unvocalized,
            SblTransliteration: "qolo",
            GrammaticalCategory: GrammaticalCategory.Noun);

        var response = await client.PostAsJsonAsync("/api/v1/admin/lexicon", body, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        return dto!.Id;
    }

    private async Task<string> UploadAsync(Guid entryId, string contentType, CancellationToken ct)
    {
        var content = new ByteArrayContent(SampleRecording());
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var form = new MultipartFormDataContent { { content, "file", $"recording{Guid.NewGuid():N}" } };

        var response = await client.PostAsync($"/api/v1/admin/lexicon/{entryId}/pronunciation", form, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"{contentType} is an accepted upload type");

        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        var url = dto!.PronunciationAudioUrl;
        url.Should().NotBeNullOrWhiteSpace();

        uploadedUrls.Add(url!);
        return url!;
    }
}
