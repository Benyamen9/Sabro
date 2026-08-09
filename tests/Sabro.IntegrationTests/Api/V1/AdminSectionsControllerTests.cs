using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Sabro.BethGazo.Application.Chants;
using Sabro.BethGazo.Domain;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Section editing under <c>/api/v1/admin/chants/sections</c>.
/// </summary>
/// <remarks>
/// <para>
/// The sections were deliberately modelled as a reference table rather than an enum —
/// "a row an editor adds, not a deploy" — and then shipped read-only, so every
/// correction was a migration. On 2026-08-08 that cost four deploys in a day. These
/// tests cover the door that was missing, and weight themselves toward the guards
/// rather than the happy path: the ways an edit can quietly corrupt chants that
/// already exist.
/// </para>
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class AdminSectionsControllerTests : IDisposable
{
    private const string Incipit = "ܡܪܝܡ";

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AdminSectionsControllerTests(PostgresFixture postgres)
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
    public async Task Post_AppendsAfterTheLastSection()
    {
        // Position is never sent by the client: the column is uniquely indexed, so
        // letting an editor type one would hand them a constraint violation whenever
        // they picked a slot already in use.
        var ct = TestContext.Current.CancellationToken;
        var before = await SectionsAsync(ct);

        var created = await CreateAsync("Appended section", [], ct);

        created.Position.Should().BeGreaterThan(before.Max(s => s.Position));
        created.AllowedModeIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_WithNoModes_IsAMeaningfulSection()
    {
        // An empty set declares a section with no modes — how the madroshe are
        // expressed — rather than a field left blank.
        var ct = TestContext.Current.CancellationToken;

        var created = await CreateAsync("Modeless section", [], ct);

        created.AllowedModeIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_WithADuplicateName_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        await CreateAsync("Twin name section", [], ct);

        var second = await client.PostAsJsonAsync(
            "/api/v1/admin/chants/sections",
            new SectionRequest("Twin name section", []),
            ct);

        second.StatusCode.Should().Be(
            HttpStatusCode.Conflict,
            "the unique name must surface as a conflict, not a 500");
    }

    [Fact]
    public async Task Post_WithAnUnknownMode_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants/sections",
            new SectionRequest("Unknown mode section", [Guid.NewGuid()]),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_ReplacesTheAdmittedModes()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = await ModeIdAsync(1, ct);
        var second = await ModeIdAsync(2, ct);
        var section = await CreateAsync("Remodelled section", [first], ct);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/chants/sections/{section.Id}",
            new SectionRequest("Remodelled section", [first, second]),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BethGazoSectionDto>(SabroApiFactory.JsonOptions, ct);
        dto!.AllowedModeIds.Should().BeEquivalentTo([first, second]);
    }

    [Fact]
    public async Task Put_RemovingAModeAChantStillUses_Returns409()
    {
        // The guard that matters most. Taking a mode away from a section that still
        // has chants using it would leave those chants holding a mode their section
        // says cannot exist — exactly the state Chant.Normalize refuses on write. The
        // data must not be allowed to drift into something the domain would reject.
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(3, ct);
        var section = await CreateAsync("Occupied section", [modeId], ct);

        await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Occupying melody", section.Id, modeId),
            ct);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/admin/chants/sections/{section.Id}",
            new SectionRequest("Occupied section", []),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_WithChantsInIt_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        var modeId = await ModeIdAsync(1, ct);
        var section = await CreateAsync("Populated section", [modeId], ct);

        await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, "Populating melody", section.Id, modeId),
            ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/sections/{section.Id}", ct);

        response.StatusCode.Should().Be(
            HttpStatusCode.Conflict,
            "the Restrict foreign key must surface as a conflict with an explanation");
    }

    [Fact]
    public async Task Delete_AnEmptySection_Succeeds()
    {
        var ct = TestContext.Current.CancellationToken;
        var section = await CreateAsync("Disposable section", [], ct);

        var response = await client.DeleteAsync($"/api/v1/admin/chants/sections/{section.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SectionsAsync(ct)).Should().NotContain(s => s.Id == section.Id);
    }

    [Fact]
    public async Task Move_SwapsWithItsNeighbour()
    {
        // Three writes, not two: Position is uniquely indexed, so one row has to park
        // outside the range before the other can take its slot. If this ever starts
        // returning 500, that dance has been "simplified" into a collision.
        var ct = TestContext.Current.CancellationToken;
        var first = await CreateAsync("Move me", [], ct);
        var second = await CreateAsync("Move past me", [], ct);

        var response = await client.PostAsync(
            $"/api/v1/admin/chants/sections/{second.Id}/move?up=true", null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await SectionsAsync(ct);
        after.Single(s => s.Id == second.Id).Position.Should().Be(first.Position);
        after.Single(s => s.Id == first.Id).Position.Should().Be(second.Position);
    }

    [Fact]
    public async Task Move_PastTheEnd_IsANoOp()
    {
        // The button is a no-op at the end rather than an error: shouting about a
        // press that changed nothing would be worse than doing nothing.
        var ct = TestContext.Current.CancellationToken;
        var sections = await SectionsAsync(ct);
        var firstId = sections.OrderBy(s => s.Position).First().Id;

        var response = await client.PostAsync(
            $"/api/v1/admin/chants/sections/{firstId}/move?up=true", null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<BethGazoSectionDto> CreateAsync(
        string name,
        IReadOnlyList<Guid> modeIds,
        CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants/sections", new SectionRequest(name, modeIds), ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<BethGazoSectionDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<IReadOnlyList<BethGazoSectionDto>> SectionsAsync(CancellationToken ct) =>
        (await client.GetFromJsonAsync<IReadOnlyList<BethGazoSectionDto>>(
            "/api/v1/admin/chants/sections", SabroApiFactory.JsonOptions, ct))!;

    private async Task<Guid> ModeIdAsync(int position, CancellationToken ct)
    {
        await using var ctx = postgres.CreateBethGazoContext();
        var mode = await ctx.Modes.AsNoTracking().FirstOrDefaultAsync(m => m.Position == position, ct);
        mode.Should().NotBeNull($"the migration seeds a mode at position {position}");
        return mode!.Id;
    }
}
