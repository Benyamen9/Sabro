using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;
using Sabro.Reviews.Application.SuggestedEdits;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Localization;
using Sabro.Shared.Results;

namespace Sabro.IntegrationTests.Reviews.Application;

/// <summary>
/// The reviewer workflow for field targets: a reviewer proposes one field of a
/// Lexicon entry or a historical figure, and only the Owner decides.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class FieldProposalServiceTests
{
    private readonly PostgresFixture postgres;

    public FieldProposalServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task Propose_AsLexiconReviewer_RecordsTimestampReadFromTheOwningModule()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var targetId = Guid.NewGuid();
        var targetUpdatedAt = DateTimeOffset.UtcNow.AddDays(-3);
        var source = FakeSource.Lexicon(targetId, targetUpdatedAt);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx, source);

        var result = await service.ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry,
                targetId,
                Field: "meaning.fr",
                ProposedValue: "parole",
                Rationale: "The current gloss renders the Greek."),
            reviewer,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Pending);
        result.Value.Field.Should().Be("meaning.fr");

        // Read server-side, never taken from the request — a caller who could set it
        // could hide that they are proposing against content which has since moved.
        result.Value.TargetUpdatedAt.Should().BeCloseTo(targetUpdatedAt, TimeSpan.FromSeconds(1));
        result.Value.TargetVersion.Should().BeNull();
    }

    [Theory]
    [InlineData("status")]
    [InlineData("playableInMeltho")]
    public async Task Propose_ForAPublicationField_IsRejected(string field)
    {
        // The rule that matters: publishing an entry and putting a word into Meltho's
        // pool are Owner-only decisions. A reviewer cannot even ask for them, because
        // those fields are absent from the owning module's proposable list.
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var targetId = Guid.NewGuid();

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx, FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow));

        var result = await service.ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry,
                targetId,
                Field: field,
                ProposedValue: "Published"),
            reviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");

        await using var verify = postgres.CreateReviewsContext();
        (await verify.SuggestedEdits.CountAsync(e => e.TargetId == targetId, ct)).Should().Be(0);
    }

    [Fact]
    public async Task Propose_ByAReviewerOfAnotherArea_IsForbidden()
    {
        // A Shmo reviewer must not reach into the Lexicon. Area separation is the
        // entire reason the roles exist.
        var ct = TestContext.Current.CancellationToken;
        var shmoReviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var targetId = Guid.NewGuid();

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx, FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow));

        var result = await service.ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry,
                targetId,
                Field: "meaning.fr",
                ProposedValue: "parole"),
            shmoReviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Propose_ByAnyoneButTheAreaReviewer_IsForbidden()
    {
        // A plain reader, an editor of the same area, and the Owner all refused. An
        // editor changes the entry directly, so a proposal from one would be a
        // decision waiting on its own author; the Owner is not a reviewer of their
        // own work.
        var ct = TestContext.Current.CancellationToken;
        var callers = new[]
        {
            await SeedProfileAsync(Role.Reader, ct),
            await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Editor)),
            await SeedProfileAsync(Role.Owner, ct),
        };
        var targetId = Guid.NewGuid();

        foreach (var caller in callers)
        {
            await using var ctx = postgres.CreateReviewsContext();
            var service = NewService(ctx, FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow));

            var result = await service.ProposeFieldChangeAsync(
                new CreateFieldProposalRequest(
                    SuggestedEditTargetType.LexiconEntry,
                    targetId,
                    Field: "meaning.fr",
                    ProposedValue: "parole"),
                caller,
                ct);

            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("forbidden");
        }
    }

    [Fact]
    public async Task Propose_AgainstAMissingTarget_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));

        await using var ctx = postgres.CreateReviewsContext();

        // The source reports no timestamp for this id, which is how a module says
        // "no such target".
        var service = NewService(ctx, FakeSource.Figure(Guid.NewGuid(), DateTimeOffset.UtcNow));

        var result = await service.ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                Guid.NewGuid(),
                Field: "era",
                ProposedValue: "451"),
            reviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task Propose_WhenNoModuleClaimsTheTargetType_IsRejected()
    {
        // Nothing registered for HistoricalFigure: the workflow must refuse rather
        // than record a proposal nobody can resolve.
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                Guid.NewGuid(),
                Field: "era",
                ProposedValue: "451"),
            reviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task AcceptWithApply_WritesTheValueThroughTheOwningModule()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Figure(targetId, DateTimeOffset.UtcNow);
        source.Values["era"] = "450";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                targetId,
                Field: "era",
                ProposedValue: "451"),
            reviewer,
            ct);
        proposed.IsSuccess.Should().BeTrue();

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest(Apply: true),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Accepted);

        // Reviews never writes another module's content itself — it hands the value to
        // the owning module, which applies it through its own write path.
        source.Applied.Should().ContainSingle();
        source.Applied[0].Should().Be(("era", "451"));
    }

    [Fact]
    public async Task Accept_WithoutApply_LeavesTheTargetUntouched()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Figure(targetId, DateTimeOffset.UtcNow);

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                targetId,
                Field: "era",
                ProposedValue: "451"),
            reviewer,
            ct);

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest(),
            owner,
            ct);

        // The default is still decide-only: the two-step path has to keep working for
        // a value the Owner wants to see in context before committing.
        result.IsSuccess.Should().BeTrue();
        source.Applied.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptWithApply_WhenTheWriteFails_RecordsNoDecision()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Figure(targetId, DateTimeOffset.UtcNow);

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                targetId,
                Field: "era",
                ProposedValue: "not-a-year"),
            reviewer,
            ct);

        source.ApplyError = Error.Validation("that is not a valid era.");

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest(Apply: true),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();

        // An accepted proposal whose value was refused would be a decision the content
        // does not reflect. The proposal stays pending instead.
        await using var readCtx = postgres.CreateReviewsContext();
        var stored = await NewService(readCtx, source).GetByIdAsync(proposed.Value!.Id, ct);
        stored.Value!.Status.Should().Be(SuggestedEditStatus.Pending);
    }

    [Fact]
    public async Task AcceptWithApply_OnAChangedField_RefusesBeforeWritingAnything()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Figure(targetId, DateTimeOffset.UtcNow);
        source.Values["era"] = "450";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                targetId,
                Field: "era",
                ProposedValue: "451"),
            reviewer,
            ct);

        // Somebody edited the same field while the proposal waited.
        source.Values["era"] = "452";

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest(Apply: true),
            owner,
            ct);

        // The one-click path must not be a way round the staleness guard: it refuses
        // exactly as decide-only does, and writes nothing.
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
        source.Applied.Should().BeEmpty();
    }

    [Fact]
    public async Task List_NamesEachTarget_InOneLookupPerModule()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var targetId = Guid.NewGuid();
        var source = FakeSource.Figure(targetId, DateTimeOffset.UtcNow);
        source.Label = new ProposalTargetLabel("Jacob of Serugh");

        await using var seedCtx = postgres.CreateReviewsContext();
        foreach (var value in new[] { "451", "452" })
        {
            var filed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
                new CreateFieldProposalRequest(
                    SuggestedEditTargetType.HistoricalFigure,
                    targetId,
                    Field: "era",
                    ProposedValue: value),
                reviewer,
                ct);
            filed.IsSuccess.Should().BeTrue();
        }

        source.LabelBatches.Clear();

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).ListAsync(
            new SuggestedEditListFilters(TargetId: targetId),
            page: 1,
            pageSize: 20,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(item => item.TargetLabel!.Primary == "Jacob of Serugh");

        // Batched: two proposals on one module cost one lookup, not one per row.
        source.LabelBatches.Should().ContainSingle();
    }

    [Fact]
    public async Task Accept_AsOwner_RecordsTheDecisionWithoutChangingTheProposal()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Shmo, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Figure(targetId, DateTimeOffset.UtcNow);

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.HistoricalFigure,
                targetId,
                Field: "era",
                ProposedValue: "451"),
            reviewer,
            ct);
        proposed.IsSuccess.Should().BeTrue();

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest("Agreed, Chalcedon."),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Accepted);
        result.Value.DecisionByLogtoUserId.Should().Be(owner);

        // Accepting is a decision, not an edit — the figure itself is untouched, and
        // the Owner applies the change through the figure's own edit path.
        result.Value.ProposedContent.Should().Be("451");
        result.Value.Field.Should().Be("era");
    }

    [Fact]
    public async Task Accept_ByTheReviewerWhoProposed_IsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var targetId = Guid.NewGuid();
        var source = FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow);

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry,
                targetId,
                Field: "syriacVocalized",
                ProposedValue: "ܡܶܠܬ݂ܳܐ"),
            reviewer,
            ct);

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest(),
            reviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Accept_WhenTheFieldChangedSinceProposal_IsRefusedByDefault()
    {
        // The regression this guard exists for: taking a correction written against
        // older content silently overwrites the newer edit. Refusing by default means
        // it cannot happen by clicking past a banner.
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow);
        source.Values["meaning.fr"] = "mot";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry, targetId, "meaning.fr", "parole"),
            reviewer,
            ct);
        proposed.Value!.OriginalValue.Should().Be("mot");

        // Somebody edits that exact field while the proposal waits.
        source.Values["meaning.fr"] = "verbe";

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value.Id, new DecisionRequest("Looks right."), owner, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");

        await using var verify = postgres.CreateReviewsContext();
        var stored = await verify.SuggestedEdits.FirstAsync(e => e.Id == proposed.Value.Id, ct);
        stored.Status.Should().Be(SuggestedEditStatus.Pending, "a refused accept must not half-apply");
    }

    [Fact]
    public async Task Accept_WhenTheFieldChanged_SucceedsWithExplicitConfirmationAndIsRecorded()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow);
        source.Values["meaning.fr"] = "mot";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry, targetId, "meaning.fr", "parole"),
            reviewer,
            ct);

        source.Values["meaning.fr"] = "verbe";

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest("Checked; the reviewer is still right.", AcceptChangedTarget: true),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Accepted);

        // Recorded, not merely allowed: "we knowingly took an older correction over a
        // newer edit" stays visible afterwards.
        result.Value.AcceptedDespiteChange.Should().BeTrue();
    }

    [Fact]
    public async Task Accept_WhenTheFieldIsUnchanged_NeedsNoConfirmation()
    {
        // The common path must stay a single click — a confirmation demanded every
        // time is one nobody reads.
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow);
        source.Values["meaning.fr"] = "mot";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry, targetId, "meaning.fr", "parole"),
            reviewer,
            ct);

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id, new DecisionRequest(), owner, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AcceptedDespiteChange.Should().BeFalse();
    }

    [Fact]
    public async Task Accept_WhenAnUnrelatedFieldChanged_IsNotTreatedAsStale()
    {
        // Why staleness is per field and not on the entity's UpdatedAt: editing the
        // English gloss must not block a pending French one. With 1,445 description
        // texts ahead, warnings that are usually wrong stop being read.
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow);
        source.Values["meaning.fr"] = "mot";
        source.Values["meaning.en"] = "word";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry, targetId, "meaning.fr", "parole"),
            reviewer,
            ct);

        source.Values["meaning.en"] = "utterance";

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).AcceptAsync(
            proposed.Value!.Id, new DecisionRequest(), owner, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AcceptedDespiteChange.Should().BeFalse();
    }

    [Fact]
    public async Task Reject_IsNeverBlockedByAChangedField()
    {
        // Rejecting writes nothing to the content, so a moved target cannot cause the
        // regression the guard protects against. Blocking it would only strand rows.
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.Reader, ct, (ContentArea.Lexicon, AreaAccess.Reviewer));
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var targetId = Guid.NewGuid();
        var source = FakeSource.Lexicon(targetId, DateTimeOffset.UtcNow);
        source.Values["meaning.fr"] = "mot";

        await using var seedCtx = postgres.CreateReviewsContext();
        var proposed = await NewService(seedCtx, source).ProposeFieldChangeAsync(
            new CreateFieldProposalRequest(
                SuggestedEditTargetType.LexiconEntry, targetId, "meaning.fr", "parole"),
            reviewer,
            ct);

        source.Values["meaning.fr"] = "verbe";

        await using var ctx = postgres.CreateReviewsContext();
        var result = await NewService(ctx, source).RejectAsync(
            proposed.Value!.Id, new DecisionRequest("Superseded."), owner, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Rejected);
    }

    [Fact]
    public void ProposableFields_ComesFromTheOwningModule()
    {
        // The backoffice picker is built from this rather than from a copy in the
        // frontend. A copy would drift silently: offering a field the API refuses,
        // or hiding one it would have taken.
        using var ctx = postgres.CreateReviewsContext();
        var source = FakeSource.Lexicon(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var result = NewService(ctx, source).GetProposableFields(SuggestedEditTargetType.LexiconEntry);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(source.ProposableFields);
        result.Value.Should().NotContain("status");
        result.Value.Should().NotContain("playableInMeltho");
    }

    [Fact]
    public void ProposableFields_ForATypeNoModuleClaims_Fails()
    {
        using var ctx = postgres.CreateReviewsContext();

        var result = NewService(ctx).GetProposableFields(SuggestedEditTargetType.HistoricalFigure);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    private static SuggestedEditService NewService(
        ReviewsDbContext ctx,
        params IProposalTargetSource[] targetSources) =>
        new(
            ctx,
            new CreateSuggestedEditRequestValidator(),
            new CreateFieldProposalRequestValidator(),
            targetSources,
            new UserProfileService(
                NewIdentityContext(ctx),
                new UpdateUserProfileRequestValidator(Options.Create(new SupportedLanguagesOptions())),
                NullLogger<UserProfileService>.Instance),
            NullLogger<SuggestedEditService>.Instance);

    private static IdentityDbContext NewIdentityContext(ReviewsDbContext reviewsContext)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(reviewsContext.Database.GetConnectionString()!)
            .Options;
        return new IdentityDbContext(options);
    }

    private async Task<string> SeedProfileAsync(
        Role role,
        CancellationToken ct,
        params (ContentArea Area, AreaAccess Access)[] grants)
    {
        var logtoUserId = $"logto|{Guid.NewGuid():N}";
        await using var identity = postgres.CreateIdentityContext();
        var profile = UserProfile.Create(logtoUserId).Value!;
        profile.AssignRole(role);
        foreach (var (area, access) in grants)
        {
            profile.SetAreaAccess(area, access);
        }

        identity.UserProfiles.Add(profile);
        await identity.SaveChangesAsync(ct);
        return logtoUserId;
    }

    /// <summary>
    /// Stands in for a content module. Real sources are backed by the Lexicon and
    /// Historical DbContexts; what matters to Reviews is only the target-type name,
    /// the proposable list, and whether a timestamp comes back.
    /// </summary>
    private sealed class FakeSource : IProposalTargetSource
    {
        private readonly Guid knownId;
        private readonly DateTimeOffset updatedAt;

        private FakeSource(string targetTypeName, string[] fields, Guid knownId, DateTimeOffset updatedAt)
        {
            TargetTypeName = targetTypeName;
            ProposableFields = fields;
            this.knownId = knownId;
            this.updatedAt = updatedAt;
        }

        public string TargetTypeName { get; }

        public IReadOnlyCollection<string> ProposableFields { get; }

        /// <summary>Current value per field, mutable so a test can change it mid-flight.</summary>
        public Dictionary<string, string?> Values { get; } = new(StringComparer.Ordinal);

        /// <summary>Every apply that got through, in order.</summary>
        public List<(string Field, string Value)> Applied { get; } = [];

        /// <summary>Set to make the next apply fail, standing in for a validation refusal.</summary>
        public Error? ApplyError { get; set; }

        /// <summary>How many ids each label lookup was asked for — one entry per call.</summary>
        public List<int> LabelBatches { get; } = [];

        public ProposalTargetLabel Label { get; set; } = new("label", "secondary");

        /// <summary>Mirrors the real Lexicon list — note the absence of status/playable.</summary>
        public static FakeSource Lexicon(Guid knownId, DateTimeOffset updatedAt) => new(
            "LexiconEntry",
            ["syriacUnvocalized", "syriacVocalized", "sblTransliteration", "meaning.en", "meaning.fr"],
            knownId,
            updatedAt);

        public static FakeSource Figure(Guid knownId, DateTimeOffset updatedAt) => new(
            "HistoricalFigure",
            ["name", "era", "period", "region", "description.en"],
            knownId,
            updatedAt);

        public Task<DateTimeOffset?> GetUpdatedAtAsync(Guid targetId, CancellationToken cancellationToken) =>
            Task.FromResult(targetId == knownId ? updatedAt : (DateTimeOffset?)null);

        public Task<string?> GetFieldValueAsync(Guid targetId, string field, CancellationToken cancellationToken) =>
            Task.FromResult(targetId == knownId && Values.TryGetValue(field, out var value) ? value : null);

        public Task<IReadOnlyDictionary<Guid, ProposalTargetLabel>> GetLabelsAsync(
            IReadOnlyCollection<Guid> targetIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, ProposalTargetLabel> labels = targetIds.Contains(knownId)
                ? new Dictionary<Guid, ProposalTargetLabel> { [knownId] = Label }
                : new Dictionary<Guid, ProposalTargetLabel>();
            LabelBatches.Add(targetIds.Count);
            return Task.FromResult(labels);
        }

        /// <summary>Records what was actually written, so a test can assert the value landed.</summary>
        public Task<Error?> ApplyFieldAsync(
            Guid targetId,
            string field,
            string value,
            CancellationToken cancellationToken)
        {
            if (targetId != knownId)
            {
                return Task.FromResult<Error?>(Error.NotFound($"{TargetTypeName} {targetId}"));
            }

            if (ApplyError is not null)
            {
                return Task.FromResult<Error?>(ApplyError);
            }

            Applied.Add((field, value));
            Values[field] = value;
            return Task.FromResult<Error?>(null);
        }
    }
}
