using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.API.Controllers.V1;
using Sabro.BethGazo.Domain;
using Sabro.Historical.Domain;
using Sabro.Lexicon.Domain;
using Sabro.Library.Application.Reading;
using Sabro.Library.Domain;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The reader's own Library at <c>/api/v1/library/me</c>: the red ribbon per book and the ܀ marks.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class ReaderControllerTests : IDisposable
{
    private const string Base = "/api/v1/library/me";

    /// <summary>The seeded Farde section, which admits every mode.</summary>
    private static readonly Guid Farde = Guid.Parse("7a2c4b20-0000-4000-8000-000000000001");

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public ReaderControllerTests(PostgresFixture postgres)
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
    public async Task Get_ForANewReader_IsAnEmptyShelf()
    {
        var ct = TestContext.Current.CancellationToken;

        var shelf = await GetShelfAsync(NewReader(), ct);

        shelf.Places.Should().BeEmpty();
        shelf.Marks.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPlace_ThenGet_ShowsTheRibbonWithTheWordsLabel()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        var word = await SeedWordAsync(publish: true, ct);

        var put = await SendAsync(HttpMethod.Put, $"{Base}/places/lexicon?language=fr", reader, new SetReadingPlaceRequest(word.ToString()), ct);

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var shelf = await GetShelfAsync(reader, ct, "?language=fr");
        var place = shelf.Places.Should().ContainSingle().Subject;
        place.Book.Should().Be(LibraryBooks.Lexicon);
        place.ItemKey.Should().Be(word.ToString("D"));
        place.Label!.Title.Should().Be("glose", "the gloss is in the language asked for");
        place.Label.Syriac.Should().Be("ܡܠܬܐ");
    }

    [Fact]
    public async Task SetPlace_MovesTheOneRibbonInThatBook()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();

        await PutPlaceAsync(reader, "grammar", "past", null, ct);
        await PutPlaceAsync(reader, "grammar", "present", null, ct);

        var shelf = await GetShelfAsync(reader, ct);
        var place = shelf.Places.Should().ContainSingle().Subject;
        place.ItemKey.Should().Be("present");
        place.Label.Should().BeNull("a grammar page is named by the hub");
    }

    [Fact]
    public async Task SetPlace_SentLateFromAnotherDevice_DoesNotMoveANewerRibbon()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        await PutPlaceAsync(reader, "grammar", "present", null, ct);

        var response = await PutPlaceAsync(reader, "grammar", "past", DateTimeOffset.UtcNow.AddDays(-1), ct);

        var kept = await response.Content.ReadFromJsonAsync<ReaderPlaceDto>(SabroApiFactory.JsonOptions, ct);
        kept!.ItemKey.Should().Be("present", "the most recently opened place wins");
    }

    [Fact]
    public async Task SetPlace_ForAnUnpublishedWord_Is404()
    {
        var ct = TestContext.Current.CancellationToken;
        var draft = await SeedWordAsync(publish: false, ct);

        var response = await SendAsync(HttpMethod.Put, $"{Base}/places/lexicon", NewReader(), new SetReadingPlaceRequest(draft.ToString()), ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetPlace_InAnUnknownBook_Is400()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await SendAsync(HttpMethod.Put, $"{Base}/places/hymnal", NewReader(), new SetReadingPlaceRequest("past"), ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("book");
    }

    [Fact]
    public async Task Mark_IsCreatedOnce_AndAgainIsHarmless()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        var figure = await SeedFigureAsync(ct);

        var first = await SendAsync(HttpMethod.Put, $"{Base}/marks/beth-salme/{figure}", reader, null, ct);
        var second = await SendAsync(HttpMethod.Put, $"{Base}/marks/beth-salme/{figure}", reader, null, ct);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var shelf = await GetShelfAsync(reader, ct);
        var mark = shelf.Marks.Should().ContainSingle().Subject;
        mark.Label!.Title.Should().StartWith("Marked Figure");
        mark.Label.Syriac.Should().Be("ܐܦܪܝܡ");
    }

    [Fact]
    public async Task Mark_KeepsTheDateOfAMarkMadeSignedOut_AndListsNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        var lastWeek = DateTimeOffset.UtcNow.AddDays(-7);
        await SendAsync(HttpMethod.Put, $"{Base}/marks/grammar/past", reader, new MarkItemRequest(lastWeek), ct);
        await SendAsync(HttpMethod.Put, $"{Base}/marks/grammar/present", reader, null, ct);

        var shelf = await GetShelfAsync(reader, ct);

        shelf.Marks.Select(m => m.ItemKey).Should().Equal("present", "past");
        shelf.Marks[1].MarkedAt.Should().BeCloseTo(lastWeek, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Mark_ABookedChant_IsLabelledWithItsMelodyAndMode()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        var (chant, melody) = await SeedRecordedChantAsync(ct);

        var response = await SendAsync(HttpMethod.Put, $"{Base}/marks/beth-gazo/{chant}", reader, null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mark = await response.Content.ReadFromJsonAsync<ReaderMarkDto>(SabroApiFactory.JsonOptions, ct);
        mark!.Label!.Title.Should().Be(melody);
        mark.Label.Subtitle.Should().NotBeNullOrEmpty("the mode tells one chant of a melody from another");
    }

    [Fact]
    public async Task Unmark_RemovesTheMark_AndAgainIsHarmless()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        await SendAsync(HttpMethod.Put, $"{Base}/marks/grammar/past", reader, null, ct);

        var first = await SendAsync(HttpMethod.Delete, $"{Base}/marks/grammar/past", reader, null, ct);
        var second = await SendAsync(HttpMethod.Delete, $"{Base}/marks/grammar/past", reader, null, ct);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetShelfAsync(reader, ct)).Marks.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_HidesAMarkWhoseWordWasUnpublished_ButKeepsItsRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        var word = await SeedWordAsync(publish: true, ct);
        await SendAsync(HttpMethod.Put, $"{Base}/marks/lexicon/{word}", reader, null, ct);

        await using (var lexicon = postgres.CreateLexiconContext())
        {
            var entry = await lexicon.Entries.SingleAsync(e => e.Id == word, ct);
            entry.ReturnToDraft();
            await lexicon.SaveChangesAsync(ct);
        }

        (await GetShelfAsync(reader, ct)).Marks.Should().BeEmpty();
        await using var library = postgres.CreateLibraryContext();
        (await library.ReadingMarks.CountAsync(m => m.LogtoUserId == reader, ct)).Should().Be(1);
    }

    [Fact]
    public async Task OneReadersShelf_IsNeverAnothers()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        await PutPlaceAsync(reader, "grammar", "past", null, ct);
        await SendAsync(HttpMethod.Put, $"{Base}/marks/grammar/past", reader, null, ct);

        var other = await GetShelfAsync(NewReader(), ct);

        other.Places.Should().BeEmpty();
        other.Marks.Should().BeEmpty();
    }

    [Fact]
    public async Task ProfileExport_IncludesTheLibrary()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = NewReader();
        await PutPlaceAsync(reader, "grammar", "past", null, ct);
        await SendAsync(HttpMethod.Put, $"{Base}/marks/grammar/present", reader, null, ct);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/profile/me/export", reader, null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var export = await response.Content.ReadFromJsonAsync<ProfileExportDto>(SabroApiFactory.JsonOptions, ct);
        export!.Library.Places.Should().ContainSingle(p => p.ItemKey == "past");
        export.Library.Marks.Should().ContainSingle(m => m.ItemKey == "present");
    }

    private static string NewReader() => $"reader-{Guid.NewGuid():N}";

    private Task<HttpResponseMessage> PutPlaceAsync(string reader, string book, string itemKey, DateTimeOffset? openedAt, CancellationToken ct) =>
        SendAsync(HttpMethod.Put, $"{Base}/places/{book}", reader, new SetReadingPlaceRequest(itemKey, openedAt), ct);

    private async Task<ReaderShelfDto> GetShelfAsync(string reader, CancellationToken ct, string query = "")
    {
        var response = await SendAsync(HttpMethod.Get, Base + query, reader, null, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ReaderShelfDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string reader, object? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.UserHeaderName, reader);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: SabroApiFactory.JsonOptions);
        }

        return await client.SendAsync(request, ct);
    }

    private async Task<Guid> SeedWordAsync(bool publish, CancellationToken ct)
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "word").Value!,
            LexiconMeaning.Create("fr", "glose").Value!,
            LexiconMeaning.Create("nl", "woord").Value!,
            LexiconMeaning.Create("de", "Wort").Value!,
            LexiconMeaning.Create("sv", "ord").Value!,
        };
        var entry = LexiconEntry.Create("ܡܠܬܐ", "meltho", GrammaticalCategory.Noun, meanings: meanings).Value!;
        if (publish)
        {
            entry.Publish().Should().BeNull();
        }

        await using var ctx = postgres.CreateLexiconContext();
        ctx.Entries.Add(entry);
        await ctx.SaveChangesAsync(ct);
        return entry.Id;
    }

    private async Task<Guid> SeedFigureAsync(CancellationToken ct)
    {
        var figure = HistoricalFigure.Create(
            $"Marked Figure {Guid.NewGuid():N}",
            HistoricalFigureCategory.Patristic,
            4, // the century, not the year
            HistoricalPeriod.NiceneEra,
            HistoricalFigureRole.Commentator,
            HistoricalFigureRegion.Mesopotamia,
            HistoricalFigureGender.Male,
            HistoricalFigureTradition.WestSyriac,
            syriacName: "ܐܦܪܝܡ").Value!;
        figure.Publish().Should().BeNull();

        await using var ctx = postgres.CreateHistoricalContext();
        ctx.Figures.Add(figure);
        await ctx.SaveChangesAsync(ct);
        return figure.Id;
    }

    private async Task<(Guid Chant, string Melody)> SeedRecordedChantAsync(CancellationToken ct)
    {
        var melody = $"Reader melody {Guid.NewGuid():N}";
        await using var ctx = postgres.CreateBethGazoContext();
        var section = await ctx.Sections.Include(s => s.AllowedModes).SingleAsync(s => s.Id == Farde, ct);
        var mode = await ctx.Modes.SingleAsync(m => m.Position == 1, ct);
        var chant = Chant.Create("ܡܪܝܡ", melody, section, mode.Id).Value!;
        chant.SetAudioUrl($"/media/chants/{Guid.NewGuid():N}.mp3").Should().BeNull();
        chant.Publish().Should().BeNull();
        ctx.Chants.Add(chant);
        await ctx.SaveChangesAsync(ct);
        return (chant.Id, melody);
    }
}
