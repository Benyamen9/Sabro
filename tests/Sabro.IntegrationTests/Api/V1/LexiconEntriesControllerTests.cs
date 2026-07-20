using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Public read surface for the Lexicon. Writes live on the admin surface
/// (see <see cref="AdminLexiconControllerTests"/>); these endpoints only ever
/// expose published entries — drafts are editorial state and 404 to clients.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class LexiconEntriesControllerTests : IDisposable
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public LexiconEntriesControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_OnMissingEntry_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync($"/api/v1/lexicon-entries/{Guid.NewGuid()}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_OnPublishedEntry_Returns200()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await SeedPublishedAsync(ct);

        var response = await client.GetAsync($"/api/v1/lexicon-entries/{id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Id.Should().Be(id);
        dto.Status.Should().Be(LexiconEntryStatus.Published);
    }

    [Fact]
    public async Task Get_OnDraftEntry_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await SeedDraftAsync(ct);

        var response = await client.GetAsync($"/api/v1/lexicon-entries/{id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_List_ReturnsOnlyPublishedEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        var publishedId = await SeedPublishedAsync(ct);
        var draftId = await SeedDraftAsync(ct);

        var response = await client.GetAsync("/api/v1/lexicon-entries?pageSize=200", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<LexiconEntryDto>>(SabroApiFactory.JsonOptions, ct);
        page!.Page.Should().Be(1);
        page.Items.Should().OnlyContain(e => e.Status == LexiconEntryStatus.Published);
        page.Items.Select(e => e.Id).Should().Contain(publishedId).And.NotContain(draftId);
    }

    [Fact]
    public async Task Get_List_WithInvalidPaging_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/lexicon-entries?page=-1&pageSize=500", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("page");
        problem.Errors.Should().ContainKey("pageSize");
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Guid> SeedPublishedAsync(CancellationToken ct)
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "to write").Value!,
            LexiconMeaning.Create("fr", "écrire").Value!,
            LexiconMeaning.Create("nl", "schrijven").Value!,
            LexiconMeaning.Create("de", "schreiben").Value!,
            LexiconMeaning.Create("sv", "skriva").Value!,
        };
        var entry = LexiconEntry.Create(KtbUnvocalized, "ktb", GrammaticalCategory.Verb, meanings: meanings).Value!;
        entry.Publish().Should().BeNull();

        await using var ctx = postgres.CreateLexiconContext();
        ctx.Entries.Add(entry);
        await ctx.SaveChangesAsync(ct);
        return entry.Id;
    }

    private async Task<Guid> SeedDraftAsync(CancellationToken ct)
    {
        var entry = LexiconEntry.Create(KtbUnvocalized, "ktb", GrammaticalCategory.Verb).Value!;

        await using var ctx = postgres.CreateLexiconContext();
        ctx.Entries.Add(entry);
        await ctx.SaveChangesAsync(ct);
        return entry.Id;
    }
}
