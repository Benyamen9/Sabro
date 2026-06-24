using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Application.Meltho;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Text;

namespace Sabro.IntegrationTests.Play.Application;

// Like MelthoPuzzleServiceTests, each test pins "today" to a distinct far-future year so the
// shared meltho_daily_puzzles table never collides and the past-only scan only sees this
// test's own rows. The lexicon reader is substituted; the real PlayDbContext exercises the
// past-only filter, per-word dedup, ordering, and pagination against Postgres.
[Collection(IntegrationCollection.Name)]
public class MelthoLibraryServiceTests
{
    private readonly PostgresFixture fixture;

    public MelthoLibraryServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task List_ExcludesTodaysWord()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2201, 6, 15);
        var yesterdayWord = Guid.NewGuid();
        var todayWord = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), yesterdayWord, ct);
        await SeedServedAsync(today, todayWord, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).ListAsync(1, 50, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle()
            .Which.LexiconEntryId.Should().Be(yesterdayWord);
    }

    [Fact]
    public async Task List_DeduplicatesWords_OrderedByMostRecentlyServed()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2211, 6, 15);
        var wordA = Guid.NewGuid();
        var wordB = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-3), wordA, ct);
        await SeedServedAsync(today.AddDays(-1), wordA, ct); // A again, more recently
        await SeedServedAsync(today.AddDays(-2), wordB, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).ListAsync(1, 50, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Select(i => i.LexiconEntryId).Should().Equal(wordA, wordB);
        result.Value.Items[0].LastPlayedOn.Should().Be(today.AddDays(-1));
    }

    [Fact]
    public async Task List_PaginatesAndReportsTotal()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2221, 6, 15);
        for (var i = 1; i <= 5; i++)
        {
            await SeedServedAsync(today.AddDays(-i), Guid.NewGuid(), ct);
        }

        await using var ctx = fixture.CreatePlayContext();
        var service = NewService(ctx, EchoReader(), today);
        var page1 = await service.ListAsync(1, 2, ct);
        var page2 = await service.ListAsync(2, 2, ct);

        page1.Value!.Total.Should().Be(5);
        page1.Value.Items.Should().HaveCount(2);
        page2.Value!.Items.Should().HaveCount(2);
        page1.Value.Items.Select(i => i.LexiconEntryId)
            .Should().NotIntersectWith(page2.Value.Items.Select(i => i.LexiconEntryId));
    }

    [Fact]
    public async Task List_InvalidPage_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx, EchoReader(), new DateOnly(2231, 6, 15)).ListAsync(0, 50, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task GetDetail_ReturnsInfoCompositionAndPastDates()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2241, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-5), word, ct);
        await SeedServedAsync(today.AddDays(-1), word, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).GetDetailAsync(word, ct);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.LexiconEntryId.Should().Be(word);
        dto.Composition.Should().NotBeEmpty();
        dto.PlayedOn.Should().Equal(today.AddDays(-1), today.AddDays(-5)); // newest first
    }

    [Fact]
    public async Task GetDetail_WordServedOnlyToday_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2251, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today, word, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).GetDetailAsync(word, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetDetail_WhenEntryMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2261, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), word, ct);

        var reader = Substitute.For<ILexiconLibraryReader>();
        reader.GetLibraryDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((LexiconLibraryDetail?)null);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, reader, today).GetDetailAsync(word, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    private static MelthoLibraryService NewService(PlayDbContext ctx, ILexiconLibraryReader reader, DateOnly today) =>
        new(ctx, reader, new FixedTimeProvider(today));

    private static ILexiconLibraryReader EchoReader()
    {
        var reader = Substitute.For<ILexiconLibraryReader>();
        reader.GetLibraryListAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci => (IReadOnlyList<LexiconLibraryListItem>)ci.Arg<IReadOnlyCollection<Guid>>()
                .Select(id => new LexiconLibraryListItem(
                    id,
                    "ܡܠܬܐ",
                    new[] { new LexiconMeaningDto("en", "word") }))
                .ToList());
        reader.GetLibraryDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new LexiconLibraryDetail(
                ci.Arg<Guid>(),
                "ܡܠܬܐ",
                "ܡܶܠܬ݂ܳܐ",
                "meltho",
                Array.Empty<string>(),
                "Noun",
                null,
                4,
                new[] { new LexiconMeaningDto("en", "word") },
                SyriacComposition.Decompose("ܡܶܠܬ݂ܳܐ")));
        return reader;
    }

    private async Task SeedServedAsync(DateOnly date, Guid entryId, CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        ctx.MelthoDailyPuzzles.Add(MelthoDailyPuzzle.Create(Games.Meltho, date, entryId).Value!);
        await ctx.SaveChangesAsync(ct);
    }
}
