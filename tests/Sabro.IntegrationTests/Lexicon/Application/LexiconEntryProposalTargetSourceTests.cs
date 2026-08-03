using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Application.Proposals;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Localization;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Lexicon.Application;

/// <summary>
/// The real Lexicon proposal source against a real database.
/// </summary>
/// <remarks>
/// These exist because the Reviews propose tests run against a `FakeSource`, so
/// this class — the one actually wired up in production — had never executed a
/// query in any test. It carried an `Include(e => e.Meanings)` that cannot work:
/// meanings are an owned collection mapped to the private `meanings` field and
/// the public property is `Ignore`d, so EF threw at query-compile time and every
/// single Lexicon proposal failed with a 500.
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class LexiconEntryProposalTargetSourceTests
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private readonly PostgresFixture fixture;

    public LexiconEntryProposalTargetSourceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetFieldValueAsync_ForAScalarField_ReturnsTheStoredValue()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var value = await NewSource(ctx).GetFieldValueAsync(id, "syriacUnvocalized", ct);

        value.Should().Be(KtbUnvocalized);
    }

    [Fact]
    public async Task GetFieldValueAsync_ForAMeaning_ReturnsThatLanguagesText()
    {
        // The meanings load with their owner; asking EF to Include them is what broke.
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var value = await NewSource(ctx).GetFieldValueAsync(id, "meaning.fr", ct);

        value.Should().Be("écrire");
    }

    [Fact]
    public async Task GetFieldValueAsync_ForAnAbsentMeaning_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var value = await NewSource(ctx).GetFieldValueAsync(id, "meaning.sv", ct);

        value.Should().BeNull();
    }

    [Fact]
    public async Task GetFieldValueAsync_ForAnUnknownEntry_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = fixture.CreateLexiconContext();
        var value = await NewSource(ctx).GetFieldValueAsync(Guid.NewGuid(), "syriacUnvocalized", ct);

        value.Should().BeNull();
    }

    [Fact]
    public async Task GetUpdatedAtAsync_ReturnsTheTimestampForAnEntryAndNullOtherwise()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var source = NewSource(ctx);

        (await source.GetUpdatedAtAsync(id, ct)).Should().NotBeNull();
        (await source.GetUpdatedAtAsync(Guid.NewGuid(), ct)).Should().BeNull();
    }

    [Fact]
    public async Task ProposableFields_OmitsTheOwnersPublicationDecisions()
    {
        // Publication and the Meltho pool stay Owner-only by being absent from this
        // list — a reviewer cannot even ask for them. Pinned so the rule cannot be
        // undone by adding a field here without noticing.
        await using var ctx = fixture.CreateLexiconContext();

        var fields = NewSource(ctx).ProposableFields;

        fields.Should().NotContain("status");
        fields.Should().NotContain("playableInMeltho");
        fields.Should().NotContain("rootId");
        fields.Should().Contain("syriacUnvocalized");
        fields.Should().Contain("meaning.en");
    }

    [Fact]
    public async Task GetLabelsAsync_NamesTheEntriesItWasAskedFor()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var labels = await NewSource(ctx).GetLabelsAsync([id, Guid.NewGuid()], ct);

        // The unknown id is simply absent rather than an error: losing a label must
        // never lose the proposal it belongs to.
        labels.Should().HaveCount(1);
        labels[id].Primary.Should().Be(KtbUnvocalized);
        labels[id].Secondary.Should().Be("ktb");
    }

    [Fact]
    public async Task GetLabelsAsync_WithNoIds_AsksTheDatabaseNothing()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = fixture.CreateLexiconContext();
        var labels = await NewSource(ctx).GetLabelsAsync([], ct);

        labels.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyFieldAsync_WritesAScalarFieldThroughTheNormalUpdatePath()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var error = await NewSource(ctx).ApplyFieldAsync(id, "sblTransliteration", "kthab", ct);

        error.Should().BeNull();

        await using var read = fixture.CreateLexiconContext();
        var value = await NewSource(read).GetFieldValueAsync(id, "sblTransliteration", ct);
        value.Should().Be("kthab");
    }

    [Fact]
    public async Task ApplyFieldAsync_ChangesOneMeaningAndLeavesTheOthersAlone()
    {
        // The update path replaces the whole meaning collection, so applying one
        // language has to carry the rest through untouched — otherwise accepting a
        // French correction would silently delete the English gloss.
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var error = await NewSource(ctx).ApplyFieldAsync(id, "meaning.fr", "rédiger", ct);

        error.Should().BeNull();

        await using var read = fixture.CreateLexiconContext();
        var source = NewSource(read);
        (await source.GetFieldValueAsync(id, "meaning.fr", ct)).Should().Be("rédiger");
        (await source.GetFieldValueAsync(id, "meaning.en", ct)).Should().Be("to write");
    }

    [Fact]
    public async Task ApplyFieldAsync_AddsAMeaningForALanguageThatHadNone()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var error = await NewSource(ctx).ApplyFieldAsync(id, "meaning.nl", "schrijven", ct);

        error.Should().BeNull();

        await using var read = fixture.CreateLexiconContext();
        (await NewSource(read).GetFieldValueAsync(id, "meaning.nl", ct)).Should().Be("schrijven");
    }

    [Fact]
    public async Task ApplyFieldAsync_RefusesAValueTheEntryWouldReject()
    {
        // Applying goes through the same validation as the backoffice form, so a
        // proposal cannot become a quieter way to write something invalid.
        var ct = TestContext.Current.CancellationToken;
        var id = await CreateEntryAsync(ct);

        await using var ctx = fixture.CreateLexiconContext();
        var error = await NewSource(ctx).ApplyFieldAsync(id, "syriacUnvocalized", "not syriac", ct);

        error.Should().NotBeNull();

        await using var read = fixture.CreateLexiconContext();
        (await NewSource(read).GetFieldValueAsync(id, "syriacUnvocalized", ct)).Should().Be(KtbUnvocalized);
    }

    [Fact]
    public async Task ApplyFieldAsync_OnAnUnknownEntry_ReportsItRatherThanThrowing()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = fixture.CreateLexiconContext();
        var error = await NewSource(ctx).ApplyFieldAsync(Guid.NewGuid(), "morphology", "x", ct);

        error.Should().NotBeNull();
        error!.Code.Should().Be("not_found");
    }

    private static LexiconEntryProposalTargetSource NewSource(LexiconDbContext ctx) =>
        new(ctx, NewEntryService(ctx), Options.Create(new SupportedLanguagesOptions()));

    private static LexiconEntryService NewEntryService(LexiconDbContext ctx) =>
        new(
            ctx,
            new CreateLexiconEntryRequestValidator(),
            new UpdateLexiconEntryRequestValidator(),
            Substitute.For<ISearchIndex<LexiconEntrySearchDocument>>(),
            Substitute.For<IPronunciationAudioStorage>(),
            Options.Create(new SupportedLanguagesOptions()),
            NullLogger<LexiconEntryService>.Instance);

    private async Task<Guid> CreateEntryAsync(CancellationToken ct)
    {
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewEntryService(ctx);

        var result = await service.CreateAsync(
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb,
                Meanings:
                [
                    new CreateLexiconMeaningRequest("en", "to write"),
                    new CreateLexiconMeaningRequest("fr", "écrire"),
                ]),
            ct);

        result.IsSuccess.Should().BeTrue();
        return result.Value!.Id;
    }
}
