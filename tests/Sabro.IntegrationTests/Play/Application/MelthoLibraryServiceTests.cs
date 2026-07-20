using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Application.Meltho;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Text;

namespace Sabro.IntegrationTests.Play.Application;

// The library list aggregates across the whole past, so — unlike the puzzle tests, which
// isolate via a unique far-future year and a 30-day window — these tests cannot tolerate
// rows left by other tests in the shared meltho_daily_puzzles table. The IntegrationCollection
// runs sequentially, so each test clears the table first to get a deterministic global view.
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
        await ClearAsync(ct);
        var today = new DateOnly(2201, 6, 15);
        var yesterdayWord = Guid.NewGuid();
        var todayWord = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), yesterdayWord, ct);
        await SeedServedAsync(today, todayWord, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).ListAsync(1, 50, LibrarySort.Recent, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle()
            .Which.LexiconEntryId.Should().Be(yesterdayWord);
    }

    [Fact]
    public async Task List_DeduplicatesWords_OrderedByMostRecentlyServed()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2211, 6, 15);
        var wordA = Guid.NewGuid();
        var wordB = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-3), wordA, ct);
        await SeedServedAsync(today.AddDays(-1), wordA, ct); // A again, more recently
        await SeedServedAsync(today.AddDays(-2), wordB, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).ListAsync(1, 50, LibrarySort.Recent, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Select(i => i.LexiconEntryId).Should().Equal(wordA, wordB);
        result.Value.Items[0].LastPlayedOn.Should().Be(today.AddDays(-1));
        result.Value.Items[0].TimesPlayed.Should().Be(2); // A was served on two past days
        result.Value.Items[1].TimesPlayed.Should().Be(1);
    }

    [Fact]
    public async Task List_PaginatesAndReportsTotal()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2221, 6, 15);
        for (var i = 1; i <= 5; i++)
        {
            await SeedServedAsync(today.AddDays(-i), Guid.NewGuid(), ct);
        }

        await using var ctx = fixture.CreatePlayContext();
        var service = NewService(ctx, EchoReader(), today);
        var page1 = await service.ListAsync(1, 2, LibrarySort.Recent, null, null, ct);
        var page2 = await service.ListAsync(2, 2, LibrarySort.Recent, null, null, ct);

        page1.Value!.Total.Should().Be(5);
        page1.Value.Items.Should().HaveCount(2);
        page2.Value!.Items.Should().HaveCount(2);
        page1.Value.Items.Select(i => i.LexiconEntryId)
            .Should().NotIntersectWith(page2.Value.Items.Select(i => i.LexiconEntryId));
    }

    [Fact]
    public async Task List_AlphabeticalSort_OrdersBySyriacFormRegardlessOfDate()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2271, 6, 15);
        var alaph = Guid.NewGuid();
        var beth = Guid.NewGuid();
        var gamal = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), gamal, ct); // most recent, but last alphabetically
        await SeedServedAsync(today.AddDays(-2), beth, ct);
        await SeedServedAsync(today.AddDays(-3), alaph, ct);

        var reader = MappedReader(new Dictionary<Guid, (string, int)>
        {
            [alaph] = ("ܐ", 1),
            [beth] = ("ܒ", 1),
            [gamal] = ("ܓ", 1),
        });

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, reader, today).ListAsync(1, 50, LibrarySort.Alphabetical, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.LexiconEntryId).Should().Equal(alaph, beth, gamal);
    }

    [Fact]
    public async Task List_LengthSort_OrdersByPlayableLengthAscending()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2281, 6, 15);
        var two = Guid.NewGuid();
        var three = Guid.NewGuid();
        var four = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), four, ct); // most recent, but longest
        await SeedServedAsync(today.AddDays(-2), three, ct);
        await SeedServedAsync(today.AddDays(-3), two, ct);

        var reader = MappedReader(new Dictionary<Guid, (string, int)>
        {
            [two] = ("ܐܐ", 2),
            [three] = ("ܒܒܒ", 3),
            [four] = ("ܓܓܓܓ", 4),
        });

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, reader, today).ListAsync(1, 50, LibrarySort.Length, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.PlayableLength).Should().Equal(2, 3, 4);
    }

    [Fact]
    public async Task List_AlphabeticalDescending_ReversesSyriacOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2291, 6, 15);
        var alaph = Guid.NewGuid();
        var beth = Guid.NewGuid();
        var gamal = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), alaph, ct);
        await SeedServedAsync(today.AddDays(-2), beth, ct);
        await SeedServedAsync(today.AddDays(-3), gamal, ct);

        var reader = MappedReader(new Dictionary<Guid, (string, int)>
        {
            [alaph] = ("ܐ", 1),
            [beth] = ("ܒ", 1),
            [gamal] = ("ܓ", 1),
        });

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, reader, today)
            .ListAsync(1, 50, LibrarySort.Alphabetical, SortDirection.Descending, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.LexiconEntryId).Should().Equal(gamal, beth, alaph);
    }

    [Fact]
    public async Task List_RecentAscending_OrdersOldestFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2301, 6, 15);
        var newest = Guid.NewGuid();
        var oldest = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), newest, ct);
        await SeedServedAsync(today.AddDays(-5), oldest, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today)
            .ListAsync(1, 50, LibrarySort.Recent, SortDirection.Ascending, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.LexiconEntryId).Should().Equal(oldest, newest);
    }

    [Fact]
    public async Task List_Search_FiltersBySyriacFormAndReportsFilteredTotal()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2311, 6, 15);
        var alaph = Guid.NewGuid();
        var beth = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), alaph, ct);
        await SeedServedAsync(today.AddDays(-2), beth, ct);

        var reader = MappedReader(new Dictionary<Guid, (string, int)>
        {
            [alaph] = ("ܐܠܦ", 3),
            [beth] = ("ܒܝܬ", 3),
        });

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, reader, today).ListAsync(1, 50, LibrarySort.Recent, null, "ܐܠܦ", ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle().Which.LexiconEntryId.Should().Be(alaph);
    }

    [Fact]
    public async Task List_Search_MatchesTransliterationIgnoringDiacritics()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2321, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), word, ct);

        var reader = Substitute.For<ILexiconLibraryReader>();
        reader.GetLibraryListAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci => (IReadOnlyList<LexiconLibraryListItem>)ci.Arg<IReadOnlyCollection<Guid>>()
                .Select(id => new LexiconLibraryListItem(id, "ܟܬܒܐ", null, "ktōbō", 4, new[] { new LexiconMeaningDto("en", "book") }))
                .ToList());

        await using var ctx = fixture.CreatePlayContext();
        var service = NewService(ctx, reader, today);

        // Query without macrons still matches; a non-matching query filters everything out.
        (await service.ListAsync(1, 50, LibrarySort.Recent, null, "ktobo", ct)).Value!.Total.Should().Be(1);
        (await service.ListAsync(1, 50, LibrarySort.Recent, null, "zzz", ct)).Value!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_InvalidPage_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx, EchoReader(), new DateOnly(2231, 6, 15)).ListAsync(0, 50, LibrarySort.Recent, null, null, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task GetDetail_ReturnsInfoCompositionAndPastDates()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2241, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-5), word, ct);
        await SeedServedAsync(today.AddDays(-1), word, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).GetDetailAsync(word, ct);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.LexiconEntryId.Should().Be(word);
        dto.Root.Should().Be("ܡܠܠ");
        dto.Composition.Should().NotBeEmpty();
        dto.PlayedOn.Should().Equal(today.AddDays(-1), today.AddDays(-5)); // newest first
    }

    [Fact]
    public async Task GetDetail_WordServedToday_ResolvesWithTodayInPlayedOn()
    {
        // The list never shows today's word, but the detail does: you can only reach it with the
        // entry id, which Meltho only hands out via today's puzzle — i.e. once you've played. So
        // a word served only today resolves (no need to wait for tomorrow), with today included.
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2251, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today, word, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).GetDetailAsync(word, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlayedOn.Should().Equal(today);
    }

    [Fact]
    public async Task GetDetail_WordNeverServed_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var today = new DateOnly(2256, 6, 15);
        var word = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(1), Guid.NewGuid(), ct); // some other future word

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, EchoReader(), today).GetDetailAsync(word, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetDetail_WhenEntryMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
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
                    "ܡܶܠܬ݂ܳܐ",
                    "meltho",
                    4,
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
                "ܡܠܠ",
                new[] { new LexiconMeaningDto("en", "word") },
                SyriacComposition.Decompose("ܡܶܠܬ݂ܳܐ")));
        return reader;
    }

    // Maps each id to a specific (Syriac form, playable length) so sort assertions are deterministic.
    private static ILexiconLibraryReader MappedReader(Dictionary<Guid, (string Syriac, int Length)> words)
    {
        var reader = Substitute.For<ILexiconLibraryReader>();
        reader.GetLibraryListAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci => (IReadOnlyList<LexiconLibraryListItem>)ci.Arg<IReadOnlyCollection<Guid>>()
                .Where(words.ContainsKey)
                .Select(id => new LexiconLibraryListItem(
                    id,
                    words[id].Syriac,
                    null,
                    null,
                    words[id].Length,
                    new[] { new LexiconMeaningDto("en", "word") }))
                .ToList());
        return reader;
    }

    private async Task ClearAsync(CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        await ctx.MelthoDailyPuzzles.ExecuteDeleteAsync(ct);
    }

    private async Task SeedServedAsync(DateOnly date, Guid entryId, CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        ctx.MelthoDailyPuzzles.Add(MelthoDailyPuzzle.Create(Games.Meltho, date, entryId).Value!);
        await ctx.SaveChangesAsync(ct);
    }
}
