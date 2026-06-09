using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Lexicon.Application;

[Collection(IntegrationCollection.Name)]
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
            .Where(e => e.SblTransliteration is not null && e.SblTransliteration.StartsWith(marker))
            .Select(e => e.SblTransliteration)
            .ToList();
        mine.Should().HaveCount(3);
        mine.Should().BeEquivalentTo(
            new[] { $"{marker}-3", $"{marker}-2", $"{marker}-1" },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task UpdateAsync_OnExistingEntry_PersistsChangesAndRecomputesLength()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateDraftAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var result = await service.UpdateAsync(
            id,
            new UpdateLexiconEntryRequest(
                SyriacUnvocalized: "ܐܒ",
                SblTransliteration: "ʾb",
                GrammaticalCategory: GrammaticalCategory.Noun,
                Meanings: AllThreeMeanings()),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SyriacUnvocalized.Should().Be("ܐܒ");
        result.Value.PlayableLength.Should().Be(2);
        result.Value.GrammaticalCategory.Should().Be(GrammaticalCategory.Noun);
        result.Value.Meanings.Should().HaveCount(3);

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Entries.FirstOrDefaultAsync(e => e.Id == id, ct);
        loaded!.SyriacUnvocalized.Should().Be("ܐܒ");
        loaded.PlayableLength.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_NonexistentId_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb),
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task UpdateAsync_OnPublishedEntry_DroppingRequiredGloss_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreatePublishedAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var result = await service.UpdateAsync(
            id,
            new UpdateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb,
                Meanings: new[] { new CreateLexiconMeaningRequest("en", "to write") }),
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task PublishAsync_WithAllRequiredGlosses_SetsPublished()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateDraftAsync(ct, AllThreeMeanings());

        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var result = await service.PublishAsync(id, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(LexiconEntryStatus.Published);
    }

    [Fact]
    public async Task PublishAsync_MissingRequiredGloss_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateDraftAsync(ct, new CreateLexiconMeaningRequest("en", "to write"));

        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var result = await service.PublishAsync(id, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task UnpublishAsync_ReturnsToDraftAndClearsPlayable()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreatePublishedAsync(ct);

        await using (var playableCtx = fixture.CreateLexiconContext())
        {
            var playableResult = await NewService(playableCtx).SetPlayableAsync(id, true, ct);
            playableResult.IsSuccess.Should().BeTrue();
        }

        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).UnpublishAsync(id, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(LexiconEntryStatus.Draft);
        result.Value.PlayableInMeltho.Should().BeFalse();
    }

    [Fact]
    public async Task SetPlayableAsync_OnDraftEntry_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateDraftAsync(ct, AllThreeMeanings());

        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).SetPlayableAsync(id, true, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task SetPlayableAsync_OnPublishedEntry_SetsFlag()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreatePublishedAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).SetPlayableAsync(id, true, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlayableInMeltho.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateDraftAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var error = await NewService(ctx).DeleteAsync(id, ct);

        error.Should().BeNull();

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Entries.FirstOrDefaultAsync(e => e.Id == id, ct);
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonexistentId_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();

        var error = await NewService(ctx).DeleteAsync(Guid.NewGuid(), ct);

        error.Should().NotBeNull();
        error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetPublishedByIdAsync_DraftEntry_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateDraftAsync(ct, AllThreeMeanings());

        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).GetPublishedByIdAsync(id, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetPublishedByIdAsync_PublishedEntry_ReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreatePublishedAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).GetPublishedByIdAsync(id, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
        result.Value.Status.Should().Be(LexiconEntryStatus.Published);
    }

    [Fact]
    public async Task ListPublishedAsync_ExcludesDrafts()
    {
        var ct = TestContext.Current.CancellationToken;
        var draftId = await CreateDraftAsync(ct, AllThreeMeanings());
        var publishedId = await CreatePublishedAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).ListPublishedAsync(page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        var ids = result.Value!.Items.Select(e => e.Id).ToList();
        ids.Should().Contain(publishedId);
        ids.Should().NotContain(draftId);
        result.Value.Items.Should().OnlyContain(e => e.Status == LexiconEntryStatus.Published);
    }

    private static CreateLexiconMeaningRequest[] AllThreeMeanings() => new[]
    {
        new CreateLexiconMeaningRequest("en", "to write"),
        new CreateLexiconMeaningRequest("fr", "écrire"),
        new CreateLexiconMeaningRequest("nl", "schrijven"),
    };

    private static LexiconEntryService NewService(Sabro.Lexicon.Infrastructure.LexiconDbContext ctx) =>
        new(
            ctx,
            new CreateLexiconEntryRequestValidator(),
            new UpdateLexiconEntryRequestValidator(),
            Substitute.For<ISearchIndex<LexiconEntrySearchDocument>>(),
            NullLogger<LexiconEntryService>.Instance);

    private async Task<Guid> CreateDraftAsync(CancellationToken ct, params CreateLexiconMeaningRequest[] meanings)
    {
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var result = await service.CreateAsync(
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb,
                Meanings: meanings.Length == 0 ? null : meanings),
            ct);
        result.IsSuccess.Should().BeTrue();
        return result.Value!.Id;
    }

    private async Task<Guid> CreatePublishedAsync(CancellationToken ct)
    {
        var id = await CreateDraftAsync(ct, AllThreeMeanings());
        await using var ctx = fixture.CreateLexiconContext();
        var result = await NewService(ctx).PublishAsync(id, ct);
        result.IsSuccess.Should().BeTrue();
        return id;
    }
}
