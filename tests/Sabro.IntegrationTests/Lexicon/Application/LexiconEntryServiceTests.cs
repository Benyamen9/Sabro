using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Lexicon.Application;

[Collection(TranslationsCollection.Name)]
public class LexiconEntryServiceTests
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private static readonly string[] TwoVariants = { "kthab", "ktab" };

    private static readonly string[] EnFrLanguages = { "en", "fr" };

    private readonly PostgresFixture fixture;

    public LexiconEntryServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithMinimalValidInput_PersistsAndReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        result.Value.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
        result.Value.Meanings.Should().BeEmpty();

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Entries.FirstOrDefaultAsync(e => e.Id == result.Value.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithMeaningsAndVariants_PersistsAllOfThem()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb,
            TransliterationVariants: TwoVariants,
            Meanings: new[]
            {
                new CreateLexiconMeaningRequest("en", "to write"),
                new CreateLexiconMeaningRequest("fr", "écrire"),
            });

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransliterationVariants.Should().BeEquivalentTo(
            TwoVariants,
            options => options.WithStrictOrdering());
        result.Value.Meanings.Should().HaveCount(2);
        result.Value.Meanings.Select(m => m.Language).Should().BeEquivalentTo(
            EnFrLanguages,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task CreateAsync_WithEmptySyriacUnvocalized_ReturnsValidationFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: string.Empty,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("syriacUnvocalized");
    }

    [Fact]
    public async Task CreateAsync_WithUndefinedCategory_ReturnsValidationFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: (GrammaticalCategory)999);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task CreateAsync_WithMeaningMissingLanguage_RejectsAtRequestValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var request = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb,
            Meanings: new[] { new CreateLexiconMeaningRequest(string.Empty, "to write") });

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_RoundTripsAllFieldsIncludingMeanings()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var created = await service.CreateAsync(
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb,
                Meanings: new[] { new CreateLexiconMeaningRequest("en", "to write") }),
            ct);

        await using var read = fixture.CreateLexiconContext();
        var readService = NewService(read);
        var fetched = await readService.GetByIdAsync(created.Value!.Id, ct);

        fetched.IsSuccess.Should().BeTrue();
        fetched.Value!.Meanings.Should().ContainSingle(m => m.Language == "en" && m.Text == "to write");
    }

    [Fact]
    public async Task ListAsync_ReturnsEntriesNewestFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var marker = $"List-{Guid.NewGuid():N}";

        await using (var ctx = fixture.CreateLexiconContext())
        {
            var service = NewService(ctx);
            for (var i = 1; i <= 3; i++)
            {
                var created = await service.CreateAsync(
                    new CreateLexiconEntryRequest(
                        SyriacUnvocalized: KtbUnvocalized,
                        SblTransliteration: $"{marker}-{i}",
                        GrammaticalCategory: GrammaticalCategory.Verb),
                    ct);
                created.IsSuccess.Should().BeTrue();
                await Task.Delay(2, ct);
            }
        }

        await using var read = fixture.CreateLexiconContext();
        var listService = NewService(read);
        var result = await listService.ListAsync(page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().BeGreaterThanOrEqualTo(3);

        var mine = result.Value.Items
            .Where(e => e.SblTransliteration.StartsWith(marker))
            .Select(e => e.SblTransliteration)
            .ToList();
        mine.Should().HaveCount(3);
        mine.Should().BeEquivalentTo(
            new[] { $"{marker}-3", $"{marker}-2", $"{marker}-1" },
            options => options.WithStrictOrdering());
    }

    private static LexiconEntryService NewService(Sabro.Lexicon.Infrastructure.LexiconDbContext ctx) =>
        new(ctx, new CreateLexiconEntryRequestValidator(), NullLogger<LexiconEntryService>.Instance);
}
