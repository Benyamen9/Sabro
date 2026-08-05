using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sabro.BethGazo.Application.Chants;
using Sabro.BethGazo.Domain;
using Sabro.IntegrationTests.Api;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The Beth Gazo backoffice under <c>/api/v1/admin/chants</c>, over HTTP against a
/// real Postgres: create, the identity constraint, the list projection, the
/// publish/playable lifecycle, the recording, and delete.
/// </summary>
/// <remarks>
/// <para>
/// Written to close a gap, not to restate the domain tests. <c>ChantService</c> had
/// unit tests around <see cref="Chant"/> and nothing that ever ran one of its
/// queries — the same shape as <c>FieldProposalServiceTests</c>'s FakeSource, which
/// let an <c>Include</c> that throws at query-compile time reach production and 500
/// every Lexicon proposal. Everything here is therefore weighted toward the parts a
/// unit test structurally cannot reach:
/// </para>
/// <list type="bullet">
/// <item>the <c>ix_chants_identity</c> unique index and the 409 the service turns it
/// into — the constraint is matched <i>by name</i> out of the driver's exception, so
/// a rename would silently become a 500;</item>
/// <item>the list projection's join to the modes table and its left join to the
/// borrowed melody, which either compile against the real provider or do not;</item>
/// <item>the <c>ILike</c> search and the delete guard's count over the self-FK.</item>
/// </list>
/// <para>
/// Every chant here carries a melody name unique to its test: the table is shared
/// across the collection, and the identity constraint is exactly what several of
/// these assert.
/// </para>
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class AdminChantsControllerTests : IDisposable
{
    private const string Incipit = "ܡܪܝܡ";

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;
    private readonly List<string> uploadedUrls = new();

    public AdminChantsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        // Recordings are written into the API project's wwwroot, which is shared
        // with the developer's own files — leave nothing behind.
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

    [Fact]
    public async Task Post_WithMinimalPayload_Returns201AndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(1, ct);

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Minimal payload melody", modeId),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.Transliteration.Should().Be("Minimal payload melody");
        dto.ModeName.Should().Be("Qadmoyo", "the projection resolves the mode id against the reference table");
        dto.Status.Should().Be(ChantStatus.Draft);
        dto.PlayableInNahlo.Should().BeFalse();
        dto.AudioUrl.Should().BeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/admin/chants/{dto.Id}");

        await using var ctx = postgres.CreateBethGazoContext();
        (await ctx.Chants.FirstOrDefaultAsync(e => e.Id == dto.Id, ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task Post_NormalizesTheSyriacToNfc()
    {
        // Decomposed input must land as NFC, or the same word typed two ways
        // becomes two different chants — and the identity constraint compares bytes.
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(1, ct);
        var decomposed = "ܡܪܝܡ̈";

        var dto = await CreateAsync(new CreateChantRequest(decomposed, "NFC melody", modeId), ct);

        dto.SyriacIncipit.Should().Be(decomposed.Normalize(NormalizationForm.FormC));
    }

    [Fact]
    public async Task Post_WithUnknownMode_Returns400()
    {
        // Checked before the domain runs, so a bad foreign key comes back as a
        // field error rather than as a constraint violation.
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Unknown mode melody", Guid.NewGuid()),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithUnknownInheritedMelody_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(1, ct);

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Orphan solqin", modeId, InheritsMelodyFromId: Guid.NewGuid()),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithNonSyriacIncipit_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(1, ct);

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest("Maryam", "Latin incipit melody", modeId),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithoutTransliteration_Returns400WithFieldError()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(1, ct);

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, string.Empty, modeId),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("transliteration");
    }

    [Fact]
    public async Task Post_SameMelodyAndModeAndShuhlofo_Returns409()
    {
        // The reason the constraint exists, and the reason the service catches it
        // by name: a raw 23505 would tell the editor nothing about which three
        // fields collided.
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(3, ct);
        var request = new CreateChantRequest(Incipit, "Duplicate identity melody", modeId, Shuhlofo: "Qadmoyo");

        await CreateAsync(request, ct);
        var second = await client.PostAsJsonAsync("/api/v1/admin/chants", request, ct);

        second.StatusCode.Should().Be(
            HttpStatusCode.Conflict,
            "the identity index must surface as a conflict, not a 500");
    }

    [Fact]
    public async Task Post_SameMelodyAndMode_DifferentShuhlofo_IsAllowed()
    {
        // Identity is the triple, not the pair. If this ever fails, the unique
        // index has been narrowed and variations of one melody can no longer
        // coexist.
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(3, ct);

        await CreateAsync(new CreateChantRequest(Incipit, "Shuhlofo pair melody", modeId, Shuhlofo: "First"), ct);
        var second = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Shuhlofo pair melody", modeId, Shuhlofo: "Second"),
            ct);

        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_SameMelodyDifferentMode_IsAllowed()
    {
        // The premise of the whole game: a melody name recurs across modes, so
        // naming the melody must not hand the player the mode.
        var ct = TestContext.Current.CancellationToken;

        await CreateAsync(new CreateChantRequest(Incipit, "Recurring melody", await ModeIdAsync(1, ct)), ct);
        var second = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Recurring melody", await ModeIdAsync(2, ct)),
            ct);

        second.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Put_ToAnExistingIdentity_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(4, ct);
        await CreateAsync(new CreateChantRequest(Incipit, "Update target melody", modeId), ct);
        var mover = await CreateAsync(new CreateChantRequest(Incipit, "Update mover melody", modeId), ct);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/chants/{mover.Id}",
            new UpdateChantRequest(Incipit, "Update target melody", modeId),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_ProjectsTheBorrowedMelody()
    {
        // The projection left-joins the parent chant. A left join that does not
        // compile takes down every read of the section at once, and only a real
        // query proves it compiles.
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(5, ct);
        var parent = await CreateAsync(new CreateChantRequest(Incipit, "Parent melody", modeId), ct);
        var solqin = await CreateAsync(
            new CreateChantRequest(Incipit, "Solqin melody", modeId, InheritsMelodyFromId: parent.Id),
            ct);

        solqin.InheritsMelodyFromId.Should().Be(parent.Id);
        solqin.InheritsMelodyFromTransliteration.Should().Be("Parent melody");

        // And a chant that carries its own melody keeps both halves null rather
        // than being dropped by the join.
        parent.InheritsMelodyFromId.Should().BeNull();
        parent.InheritsMelodyFromTransliteration.Should().BeNull();
    }

    [Fact]
    public async Task List_FindsAChantByItsTransliterationAndByItsSyriac()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(6, ct);
        var created = await CreateAsync(new CreateChantRequest("ܩܘܩܝܐ", "Searchable melody", modeId), ct);

        // Case-insensitive, and on either form — an editor searches with whichever
        // is to hand.
        var byLatin = await ListAsync("?search=searchable%20melody", ct);
        byLatin.Items.Should().Contain(c => c.Id == created.Id);

        var bySyriac = await ListAsync("?search=%DC%A9%DC%98%DC%A9%DC%9D%DC%90", ct);
        bySyriac.Items.Should().Contain(c => c.Id == created.Id);
    }

    [Fact]
    public async Task List_FiltersByStatusAndMode()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(7, ct);
        var created = await CreateAsync(new CreateChantRequest(Incipit, "Filtered melody", modeId), ct);

        var drafts = await ListAsync($"?status=Draft&modeId={modeId}&search=Filtered", ct);
        drafts.Items.Should().Contain(c => c.Id == created.Id);

        var published = await ListAsync($"?status=Published&modeId={modeId}&search=Filtered", ct);
        published.Items.Should().NotContain(c => c.Id == created.Id);
    }

    [Fact]
    public async Task GetModes_ReturnsTheSeededModesInTraditionalOrder()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/api/v1/admin/chants/modes", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var modes = await response.Content.ReadFromJsonAsync<List<BethGazoModeDto>>(SabroApiFactory.JsonOptions, ct);
        modes.Should().NotBeNull();

        // At least the eight seeded modes, in position order. Deliberately not an
        // equality check on eight: the set grows as the owner works through the
        // tradition, and some sets run past eight.
        modes!.Count.Should().BeGreaterThanOrEqualTo(8);
        modes.Select(m => m.Position).Should().BeInAscendingOrder();
        modes[0].Name.Should().Be("Qadmoyo");
    }

    [Fact]
    public async Task GetById_ForAnUnknownId_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync($"/api/v1/admin/chants/{Guid.NewGuid()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publish_WithoutARecording_Returns400()
    {
        // The publish gate: a chant with no audio is not a puzzle.
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Unrecorded melody", await ModeIdAsync(1, ct)), ct);

        var response = await client.PostAsync($"/api/v1/admin/chants/{chant.Id}/publish", null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Publish_WithARecording_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Recorded melody", await ModeIdAsync(2, ct)), ct);
        await UploadAudioAsync(chant.Id, ct);

        var response = await client.PostAsync($"/api/v1/admin/chants/{chant.Id}/publish", null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(ChantStatus.Published);
    }

    [Fact]
    public async Task SetPlayable_OnADraft_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Draft playable melody", await ModeIdAsync(3, ct)), ct);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/chants/{chant.Id}/playable",
            new SetPlayableChantRequest(true),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unpublish_ClearsThePlayableFlag()
    {
        // A draft can never be playable, so returning one to draft must take it out
        // of the pool rather than leaving a draft flagged for the daily selection.
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Round trip melody", await ModeIdAsync(4, ct)), ct);
        await UploadAudioAsync(chant.Id, ct);
        await client.PostAsync($"/api/v1/admin/chants/{chant.Id}/publish", null, ct);
        await client.PutAsJsonAsync($"/api/v1/admin/chants/{chant.Id}/playable", new SetPlayableChantRequest(true), ct);

        var response = await client.PostAsync($"/api/v1/admin/chants/{chant.Id}/unpublish", null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(ChantStatus.Draft);
        dto.PlayableInNahlo.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAudio_WhilePublished_Returns400AndKeepsTheRecording()
    {
        // A published chant without audio would sit in the pool as an unplayable
        // puzzle, so the domain refuses the removal outright.
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Published audio melody", await ModeIdAsync(5, ct)), ct);
        await UploadAudioAsync(chant.Id, ct);
        await client.PostAsync($"/api/v1/admin/chants/{chant.Id}/publish", null, ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/{chant.Id}/audio", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var reread = await client.GetAsync($"/api/v1/admin/chants/{chant.Id}", ct);
        var dto = await reread.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct);
        dto!.AudioUrl.Should().NotBeNull("the refusal must not have deleted the file anyway");
    }

    [Fact]
    public async Task UploadAudio_WithAnUnsupportedType_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Bad upload melody", await ModeIdAsync(6, ct)), ct);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 1, 2, 3 });
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", "chant.pdf");

        var response = await client.PostAsync($"/api/v1/admin/chants/{chant.Id}/audio", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_WhenAnotherChantInheritsTheMelody_Returns409()
    {
        // The count runs over the self-FK. Deleting the parent would leave the
        // solqin pointing at nothing, so it is refused with an explanation rather
        // than surfacing a foreign-key violation.
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(7, ct);
        var parent = await CreateAsync(new CreateChantRequest(Incipit, "Undeletable parent melody", modeId), ct);
        await CreateAsync(new CreateChantRequest(Incipit, "Dependent solqin melody", modeId, InheritsMelodyFromId: parent.Id), ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/{parent.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WithNothingPointingAtIt_Returns204()
    {
        var ct = TestContext.Current.CancellationToken;
        var chant = await CreateAsync(new CreateChantRequest(Incipit, "Deletable melody", await ModeIdAsync(8, ct)), ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/{chant.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var ctx = postgres.CreateBethGazoContext();
        (await ctx.Chants.FirstOrDefaultAsync(e => e.Id == chant.Id, ct)).Should().BeNull();
    }

    private async Task<ChantDto> CreateAsync(CreateChantRequest request, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/v1/admin/chants", request, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<PagedResult<ChantDto>> ListAsync(string query, CancellationToken ct)
    {
        var response = await client.GetAsync($"/api/v1/admin/chants{query}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<ChantDto>>(SabroApiFactory.JsonOptions, ct))!;
    }

    /// <summary>The seeded mode at the given traditional position, read rather than hardcoded.</summary>
    private async Task<Guid> ModeIdAsync(int position, CancellationToken ct)
    {
        await using var ctx = postgres.CreateBethGazoContext();
        var mode = await ctx.Modes.AsNoTracking().FirstOrDefaultAsync(m => m.Position == position, ct);
        mode.Should().NotBeNull($"the migration seeds a mode at position {position}");
        return mode!.Id;
    }

    private async Task UploadAudioAsync(Guid chantId, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();

        // Deliberately not a real audio stream: the endpoint validates the declared
        // content type, not the container.
        var file = new ByteArrayContent(new byte[] { 0x53, 0x41, 0x42, 0x52, 0x4F });
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(file, "file", "chant.mp3");

        var response = await client.PostAsync($"/api/v1/admin/chants/{chantId}/audio", content, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct);
        uploadedUrls.Add(dto!.AudioUrl!);
    }
}
