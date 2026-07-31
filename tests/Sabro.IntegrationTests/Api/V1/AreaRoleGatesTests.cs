using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.Identity.Domain;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The point of the area roles: a Shmo editor reaches the figures backoffice and
/// nothing else.
/// </summary>
/// <remarks>
/// Every caller here carries the full <c>api:v1:admin</c> scope — the test auth
/// handler always issues it — so anything refused below is refused by the Sabro
/// role and not by the token. That is exactly the separation being asserted:
/// before this, holding the scope meant holding everything.
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class AreaRoleGatesTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;

    public AreaRoleGatesTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
    }

    [Fact]
    public async Task ShmoEditor_MayListAndCreateFigures()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ClientWithRoleAsync("shmo-editor", Role.ShmoEditor, ct);

        (await client.GetAsync("/api/v1/admin/historical-figures", ct))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateFigureAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ShmoEditor_IsRefusedTheLexicon()
    {
        // The whole reason the roles exist: "let someone edit the characters
        // without handing over the dictionary".
        var ct = TestContext.Current.CancellationToken;
        using var client = await ClientWithRoleAsync("shmo-editor-2", Role.ShmoEditor, ct);

        (await ReadLexiconAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CreateEntryAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LexiconEditor_IsRefusedTheFigures()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ClientWithRoleAsync("lexicon-editor", Role.LexiconEditor, ct);

        (await ReadLexiconAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "404 means the gate allowed the read");
        (await client.GetAsync("/api/v1/admin/historical-figures", ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CreateFigureAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShmoReviewer_MayLookButNotTouch()
    {
        // Until the proposal machinery exists, a reviewer role is read access.
        var ct = TestContext.Current.CancellationToken;
        using var client = await ClientWithRoleAsync("shmo-reviewer", Role.ShmoReviewer, ct);

        (await client.GetAsync("/api/v1/admin/historical-figures", ct))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateFigureAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reader_IsRefusedEverything()
    {
        // Holding the admin scope is no longer sufficient on its own.
        var ct = TestContext.Current.CancellationToken;
        using var client = await ClientWithRoleAsync("plain-reader", Role.Reader, ct);

        (await ReadLexiconAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/admin/historical-figures", ct))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_MayReachBothAreas()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await ClientWithRoleAsync("the-owner", Role.Owner, ct);

        (await ReadLexiconAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "404 means the gate allowed the read");
        (await client.GetAsync("/api/v1/admin/historical-figures", ct))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateFigureAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await CreateEntryAsync(client, ct))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<HttpResponseMessage> CreateFigureAsync(HttpClient client, CancellationToken ct) =>
        await client.PostAsJsonAsync(
            "/api/v1/admin/historical-figures",
            new CreateHistoricalFigureRequest(
                Name: $"Gate probe {Guid.NewGuid():N}",
                Category: HistoricalFigureCategory.Patristic,
                Era: 7,
                Period: HistoricalPeriod.PostChalcedonian,
                Role: HistoricalFigureRole.Bishop,
                Region: HistoricalFigureRegion.Syria,
                Gender: HistoricalFigureGender.Male),
            ct);

    /// <summary>
    /// Reads one Lexicon entry that does not exist. The admin <em>list</em> is served
    /// by Meilisearch, which the fixture deliberately points at a closed port, so it
    /// answers 500 whatever the role — useless for telling an allowed caller from a
    /// refused one. A by-id read is pure relational: 404 means the gate let us
    /// through, 403 means it did not.
    /// </summary>
    private static async Task<HttpResponseMessage> ReadLexiconAsync(HttpClient client, CancellationToken ct) =>
        await client.GetAsync($"/api/v1/admin/lexicon/{Guid.NewGuid()}", ct);

    private static async Task<HttpResponseMessage> CreateEntryAsync(HttpClient client, CancellationToken ct) =>
        await client.PostAsJsonAsync(
            "/api/v1/admin/lexicon",
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: "ܟܬܒ",
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb),
            ct);

    private async Task<HttpClient> ClientWithRoleAsync(string logtoUserId, Role role, CancellationToken ct)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, logtoUserId);

        // Touching /profile/me creates the row; the role is then seeded directly,
        // since granting it over HTTP would need an Owner that these tests are not
        // about.
        (await client.GetAsync("/api/v1/profile/me", ct)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var ctx = postgres.CreateIdentityContext();
        var profile = await ctx.UserProfiles.FirstAsync(p => p.LogtoUserId == logtoUserId, ct);
        profile.AssignRole(role);
        await ctx.SaveChangesAsync(ct);

        return client;
    }
}
