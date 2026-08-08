using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Sabro.BethGazo.Application.Chants;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Mode editing under <c>/api/v1/admin/chants/modes</c>.
/// </summary>
/// <remarks>
/// The modes were made a reference table rather than an enum precisely because the
/// owner said "some have more than eight, so make sure to have some margins" — and
/// then shipped read-only, so the one thing the table existed to allow was the one
/// thing nobody could do. Adding the ninth took a code change and a deploy.
/// <para>
/// Weighted at the guards. A mode is referenced from two directions — by a chant and
/// by a section that admits it — and the second is the easy one to forget: that link
/// is Cascade, so without a guard the row would vanish silently and the section would
/// quietly narrow.
/// </para>
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class AdminModesControllerTests : IDisposable
{
    private const string Incipit = "ܡܪܝܡ";

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AdminModesControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Post_AppendsAfterTheLastMode()
    {
        var ct = TestContext.Current.CancellationToken;
        var before = await ModesAsync(ct);

        var created = await CreateAsync("Tesheʿoyo", ct);

        created.Position.Should().BeGreaterThan(before.Max(m => m.Position));
    }

    [Fact]
    public async Task Post_PastTheNinth_IsOrdinary()
    {
        // The whole reason this is a table. Nothing anywhere may assume the count
        // stops at eight, or at nine.
        var ct = TestContext.Current.CancellationToken;

        var created = await CreateAsync("Tenth mode", ct);

        created.Position.Should().BeGreaterThan(9);
    }

    [Fact]
    public async Task Post_WithADuplicateName_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        await CreateAsync("Twin mode name", ct);

        var second = await client.PostAsJsonAsync(
            "/api/v1/admin/chants/modes", new ModeRequest("Twin mode name"), ct);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Put_RenamesWithoutDisturbingChants()
    {
        // Chants and sections point at the id, never the name, so correcting a
        // transliteration must never orphan anything.
        var ct = TestContext.Current.CancellationToken;
        var mode = await CreateAsync("Mispelled mode", ct);
        var section = await CreateSectionAsync("Renaming section", [mode.Id], ct);

        await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Renaming melody", section.Id, mode.Id),
            ct);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/chants/modes/{mode.Id}", new ModeRequest("Corrected mode"), ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BethGazoModeDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Name.Should().Be("Corrected mode");
        dto.Id.Should().Be(mode.Id, "the id must survive a rename or every chant would be orphaned");
    }

    [Fact]
    public async Task Delete_WithAChantInIt_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        var mode = await CreateAsync("Occupied mode", ct);
        var section = await CreateSectionAsync("Mode-occupied section", [mode.Id], ct);

        await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Mode-occupying melody", section.Id, mode.Id),
            ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/modes/{mode.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WhileASectionStillAdmitsIt_Returns409()
    {
        // The guard that is easy to miss. The section-to-mode link is Cascade, so
        // without this the row would disappear without a word and the section would
        // silently stop offering a mode it is supposed to admit.
        var ct = TestContext.Current.CancellationToken;
        var mode = await CreateAsync("Admitted mode", ct);
        await CreateSectionAsync("Admitting section", [mode.Id], ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/modes/{mode.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_AnUnusedMode_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var mode = await CreateAsync("Disposable mode", ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/modes/{mode.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ModesAsync(ct)).Should().NotContain(m => m.Id == mode.Id);
    }

    [Fact]
    public async Task Move_SwapsWithItsNeighbour()
    {
        // Three writes: Position is uniquely indexed, so one row parks outside the
        // range before the other takes its slot. A 500 here means that was
        // "simplified" into a collision.
        var ct = TestContext.Current.CancellationToken;
        var first = await CreateAsync("Swap mode A", ct);
        var second = await CreateAsync("Swap mode B", ct);

        var response = await client.PostAsync(
            $"/api/v1/admin/chants/modes/{second.Id}/move?up=true", null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await ModesAsync(ct);
        after.Single(m => m.Id == second.Id).Position.Should().Be(first.Position);
        after.Single(m => m.Id == first.Id).Position.Should().Be(second.Position);
    }

    private async Task<BethGazoModeDto> CreateAsync(string name, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants/modes", new ModeRequest(name), ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BethGazoModeDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<BethGazoSectionDto> CreateSectionAsync(
        string name,
        IReadOnlyList<Guid> modeIds,
        CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants/sections", new SectionRequest(name, modeIds), ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BethGazoSectionDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<IReadOnlyList<BethGazoModeDto>> ModesAsync(CancellationToken ct) =>
        (await client.GetFromJsonAsync<IReadOnlyList<BethGazoModeDto>>(
            "/api/v1/admin/chants/modes", SabroApiFactory.JsonOptions, ct))!;
}
