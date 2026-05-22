using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;
using Sabro.Reviews.Application.Approvals;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Results;
using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Infrastructure;

namespace Sabro.IntegrationTests.Reviews.Application;

[Collection(IntegrationCollection.Name)]
public class ApprovalServiceTests
{
    private readonly PostgresFixture postgres;

    public ApprovalServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task Create_SegmentAsOwner_PersistsApprovedRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Segment,
                sourceId,
                ChapterNumber: 1,
                VerseNumber: 3,
                Version: 1,
                AnnotationId: null,
                ApprovalStatus.Approved,
                Note: "Reads well."),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.TargetType.Should().Be(ApprovalTargetType.Segment);
        dto.SourceId.Should().Be(sourceId);
        dto.ChapterNumber.Should().Be(1);
        dto.VerseNumber.Should().Be(3);
        dto.Version.Should().Be(1);
        dto.Status.Should().Be(ApprovalStatus.Approved);
        dto.Note.Should().Be("Reads well.");
        dto.DecisionByLogtoUserId.Should().Be(owner);

        await using var verify = postgres.CreateReviewsContext();
        (await verify.Approvals.CountAsync(a => a.Id == dto.Id, ct)).Should().Be(1);
    }

    [Fact]
    public async Task Create_ChapterAsOwner_PersistsRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Chapter,
                sourceId,
                ChapterNumber: 4,
                VerseNumber: null,
                Version: null,
                AnnotationId: null,
                ApprovalStatus.Approved),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TargetType.Should().Be(ApprovalTargetType.Chapter);
        result.Value.VerseNumber.Should().BeNull();
        result.Value.Version.Should().BeNull();
    }

    [Fact]
    public async Task Create_AsReader_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(ValidSegmentRequest(), reader, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Create_AsExpertReviewer_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(ValidSegmentRequest(), reviewer, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Create_SegmentMissingVerseNumber_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Segment,
                Guid.NewGuid(),
                ChapterNumber: 1,
                VerseNumber: null,
                Version: 1,
                AnnotationId: null,
                ApprovalStatus.Approved),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Create_ChapterWithVerseNumber_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Chapter,
                Guid.NewGuid(),
                ChapterNumber: 1,
                VerseNumber: 5,
                Version: null,
                AnnotationId: null,
                ApprovalStatus.Approved),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task GetById_OnMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.GetByIdAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task List_FiltersBySourceAndStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceA = Guid.NewGuid();
        var sourceB = Guid.NewGuid();

        await using (var seed = postgres.CreateReviewsContext())
        {
            var service = NewService(seed);
            await service.CreateAsync(SegmentRequest(sourceA, 1, 1, 1, ApprovalStatus.Approved), owner, ct);
            await service.CreateAsync(SegmentRequest(sourceA, 1, 2, 1, ApprovalStatus.Rejected), owner, ct);
            await service.CreateAsync(SegmentRequest(sourceB, 1, 1, 1, ApprovalStatus.Approved), owner, ct);
        }

        await using var ctx = postgres.CreateReviewsContext();
        var queryService = NewService(ctx);

        var result = await queryService.ListAsync(
            new ApprovalListFilters(
                Status: ApprovalStatus.Approved,
                SourceId: sourceA),
            page: 1,
            pageSize: 20,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Single().SourceId.Should().Be(sourceA);
        result.Value.Items.Single().Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public async Task List_FiltersByVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();

        await using (var seed = postgres.CreateReviewsContext())
        {
            var service = NewService(seed);
            await service.CreateAsync(SegmentRequest(sourceId, 1, 1, 1, ApprovalStatus.Approved), owner, ct);
            await service.CreateAsync(SegmentRequest(sourceId, 1, 1, 2, ApprovalStatus.Rejected), owner, ct);
            await service.CreateAsync(SegmentRequest(sourceId, 1, 1, 3, ApprovalStatus.Approved), owner, ct);
        }

        await using var ctx = postgres.CreateReviewsContext();
        var queryService = NewService(ctx);

        var result = await queryService.ListAsync(
            new ApprovalListFilters(
                SourceId: sourceId,
                Version: 2),
            page: 1,
            pageSize: 20,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Single().Version.Should().Be(2);
        result.Value.Items.Single().Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public async Task GetEffective_OnEmptyChapter_ReturnsNoApprovals()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.GetEffectiveForChapterAsync(Guid.NewGuid(), chapterNumber: 1, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChapterApproval.Should().BeNull();
        result.Value.VerseApprovals.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEffective_ReturnsLatestChapterApprovalAndLatestPerVerse()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();

        await using (var seed = postgres.CreateReviewsContext())
        {
            var service = NewService(seed);

            // Two chapter approvals — latest wins
            var oldChapter = await service.CreateAsync(
                new CreateApprovalRequest(ApprovalTargetType.Chapter, sourceId, 1, null, null, null, ApprovalStatus.Approved, "old"),
                owner,
                ct);
            await Task.Delay(10, ct);
            var newChapter = await service.CreateAsync(
                new CreateApprovalRequest(ApprovalTargetType.Chapter, sourceId, 1, null, null, null, ApprovalStatus.Rejected, "newer"),
                owner,
                ct);

            // Two approvals on verse 1 — latest wins
            await service.CreateAsync(SegmentRequest(sourceId, 1, 1, 1, ApprovalStatus.Approved, "first"), owner, ct);
            await Task.Delay(10, ct);
            await service.CreateAsync(SegmentRequest(sourceId, 1, 1, 2, ApprovalStatus.Rejected, "second"), owner, ct);

            // One approval on verse 2 — verse-level override of chapter
            await service.CreateAsync(SegmentRequest(sourceId, 1, 2, 1, ApprovalStatus.Approved, "verse2"), owner, ct);

            oldChapter.IsSuccess.Should().BeTrue();
            newChapter.IsSuccess.Should().BeTrue();
        }

        await using var ctx = postgres.CreateReviewsContext();
        var queryService = NewService(ctx);

        var result = await queryService.GetEffectiveForChapterAsync(sourceId, chapterNumber: 1, ct);

        result.IsSuccess.Should().BeTrue();
        var effective = result.Value!;
        effective.SourceId.Should().Be(sourceId);
        effective.ChapterNumber.Should().Be(1);
        effective.ChapterApproval.Should().NotBeNull();
        effective.ChapterApproval!.Status.Should().Be(ApprovalStatus.Rejected);
        effective.ChapterApproval.Note.Should().Be("newer");

        effective.VerseApprovals.Should().HaveCount(2);
        var verse1 = effective.VerseApprovals.Single(v => v.VerseNumber == 1);
        verse1.Status.Should().Be(ApprovalStatus.Rejected);
        verse1.Note.Should().Be("second");
        var verse2 = effective.VerseApprovals.Single(v => v.VerseNumber == 2);
        verse2.Status.Should().Be(ApprovalStatus.Approved);
        verse2.Note.Should().Be("verse2");
    }

    [Fact]
    public async Task GetEffective_IsolatedBySourceAndChapter()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();
        var otherSource = Guid.NewGuid();

        await using (var seed = postgres.CreateReviewsContext())
        {
            var service = NewService(seed);
            await service.CreateAsync(SegmentRequest(sourceId, 1, 1, 1, ApprovalStatus.Approved), owner, ct);
            await service.CreateAsync(SegmentRequest(sourceId, 2, 1, 1, ApprovalStatus.Approved), owner, ct);
            await service.CreateAsync(SegmentRequest(otherSource, 1, 1, 1, ApprovalStatus.Approved), owner, ct);
        }

        await using var ctx = postgres.CreateReviewsContext();
        var queryService = NewService(ctx);

        var result = await queryService.GetEffectiveForChapterAsync(sourceId, chapterNumber: 1, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VerseApprovals.Should().HaveCount(1);
        result.Value.VerseApprovals.Single().VerseNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetEffective_WithEmptySourceId_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.GetEffectiveForChapterAsync(Guid.Empty, chapterNumber: 1, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Create_AnnotationAsOwner_DenormalizesParentLocator()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var seed = await postgres.SeedAnnotationAsync(chapter: 4, verse: 9, ct);

        await using var reviews = postgres.CreateReviewsContext();
        await using var translations = postgres.CreateContext();
        var service = NewServiceWithRealLookup(reviews, translations);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Annotation,
                SourceId: null,
                ChapterNumber: null,
                VerseNumber: null,
                Version: null,
                AnnotationId: seed.AnnotationId,
                ApprovalStatus.Approved,
                Note: "Footnote ok."),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.TargetType.Should().Be(ApprovalTargetType.Annotation);
        dto.AnnotationId.Should().Be(seed.AnnotationId);
        dto.SourceId.Should().Be(seed.SourceId);
        dto.ChapterNumber.Should().Be(4);
        dto.VerseNumber.Should().Be(9);
        dto.Version.Should().Be(seed.AnnotationVersion);
        dto.Note.Should().Be("Footnote ok.");
    }

    [Fact]
    public async Task Create_AnnotationWithUnknownId_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var reviews = postgres.CreateReviewsContext();
        await using var translations = postgres.CreateContext();
        var service = NewServiceWithRealLookup(reviews, translations);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Annotation,
                SourceId: null,
                ChapterNumber: null,
                VerseNumber: null,
                Version: null,
                AnnotationId: Guid.NewGuid(),
                ApprovalStatus.Approved),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task Create_AnnotationAsReader_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);
        var seed = await postgres.SeedAnnotationAsync(chapter: 1, verse: 1, ct);

        await using var reviews = postgres.CreateReviewsContext();
        await using var translations = postgres.CreateContext();
        var service = NewServiceWithRealLookup(reviews, translations);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Annotation,
                SourceId: null,
                ChapterNumber: null,
                VerseNumber: null,
                Version: null,
                AnnotationId: seed.AnnotationId,
                ApprovalStatus.Approved),
            reader,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Create_AnnotationWithExtraLocatorFields_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Annotation,
                SourceId: Guid.NewGuid(),
                ChapterNumber: null,
                VerseNumber: null,
                Version: null,
                AnnotationId: Guid.NewGuid(),
                ApprovalStatus.Approved),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved, AnnotationApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected, AnnotationApprovalStatus.Rejected)]
    public async Task Create_AnnotationAsOwner_NotifiesAnnotationApprovalIndexer(
        ApprovalStatus reviewsStatus,
        AnnotationApprovalStatus expectedIndexerStatus)
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var seed = await postgres.SeedAnnotationAsync(chapter: 2, verse: 5, ct);
        var indexer = new FakeAnnotationApprovalIndexer();

        await using var reviews = postgres.CreateReviewsContext();
        await using var translations = postgres.CreateContext();
        var service = NewServiceWithRealLookup(reviews, translations, indexer);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Annotation,
                SourceId: null,
                ChapterNumber: null,
                VerseNumber: null,
                Version: null,
                AnnotationId: seed.AnnotationId,
                reviewsStatus),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        indexer.Calls.Should().ContainSingle()
            .Which.Should().Be((seed.AnnotationId, expectedIndexerStatus));
    }

    [Fact]
    public async Task Create_SegmentApproval_DoesNotNotifyAnnotationApprovalIndexer()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var indexer = new FakeAnnotationApprovalIndexer();

        await using var reviews = postgres.CreateReviewsContext();
        await using var translations = postgres.CreateContext();
        var service = NewServiceWithRealLookup(reviews, translations, indexer);

        var result = await service.CreateAsync(
            new CreateApprovalRequest(
                ApprovalTargetType.Segment,
                SourceId: Guid.NewGuid(),
                ChapterNumber: 1,
                VerseNumber: 1,
                Version: 1,
                AnnotationId: null,
                ApprovalStatus.Approved),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        indexer.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEffective_IncludesLatestAnnotationApprovals()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var seed = await postgres.SeedAnnotationAsync(chapter: 6, verse: 2, ct);

        await using (var reviews = postgres.CreateReviewsContext())
        await using (var translations = postgres.CreateContext())
        {
            var service = NewServiceWithRealLookup(reviews, translations);
            var first = await service.CreateAsync(
                new CreateApprovalRequest(
                    ApprovalTargetType.Annotation,
                    SourceId: null,
                    ChapterNumber: null,
                    VerseNumber: null,
                    Version: null,
                    AnnotationId: seed.AnnotationId,
                    ApprovalStatus.Approved,
                    Note: "first"),
                owner,
                ct);
            first.IsSuccess.Should().BeTrue();

            await Task.Delay(10, ct);

            var second = await service.CreateAsync(
                new CreateApprovalRequest(
                    ApprovalTargetType.Annotation,
                    SourceId: null,
                    ChapterNumber: null,
                    VerseNumber: null,
                    Version: null,
                    AnnotationId: seed.AnnotationId,
                    ApprovalStatus.Rejected,
                    Note: "second"),
                owner,
                ct);
            second.IsSuccess.Should().BeTrue();
        }

        await using var queryCtx = postgres.CreateReviewsContext();
        var queryService = NewService(queryCtx);

        var result = await queryService.GetEffectiveForChapterAsync(seed.SourceId, chapterNumber: 6, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AnnotationApprovals.Should().HaveCount(1);
        var latest = result.Value.AnnotationApprovals.Single();
        latest.AnnotationId.Should().Be(seed.AnnotationId);
        latest.Status.Should().Be(ApprovalStatus.Rejected);
        latest.Note.Should().Be("second");
    }

    private static CreateApprovalRequest ValidSegmentRequest() =>
        new(
            ApprovalTargetType.Segment,
            Guid.NewGuid(),
            ChapterNumber: 1,
            VerseNumber: 1,
            Version: 1,
            AnnotationId: null,
            ApprovalStatus.Approved);

    private static CreateApprovalRequest SegmentRequest(
        Guid sourceId,
        int chapter,
        int verse,
        int version,
        ApprovalStatus status,
        string? note = null) =>
        new(
            ApprovalTargetType.Segment,
            sourceId,
            chapter,
            verse,
            version,
            AnnotationId: null,
            status,
            note);

    private static ApprovalService NewService(ReviewsDbContext ctx) =>
        new(
            ctx,
            new CreateApprovalRequestValidator(),
            new UserProfileService(
                NewIdentityContext(ctx),
                new UpdateUserProfileRequestValidator(),
                NullLogger<UserProfileService>.Instance),
            new FakeAnnotationLookup(),
            new FakeAnnotationApprovalIndexer(),
            NullLogger<ApprovalService>.Instance);

    private static ApprovalService NewServiceWithRealLookup(
        ReviewsDbContext reviewsCtx,
        TranslationsDbContext translationsCtx) =>
        NewServiceWithRealLookup(reviewsCtx, translationsCtx, new FakeAnnotationApprovalIndexer());

    private static ApprovalService NewServiceWithRealLookup(
        ReviewsDbContext reviewsCtx,
        TranslationsDbContext translationsCtx,
        IAnnotationApprovalIndexer indexer) =>
        new(
            reviewsCtx,
            new CreateApprovalRequestValidator(),
            new UserProfileService(
                NewIdentityContext(reviewsCtx),
                new UpdateUserProfileRequestValidator(),
                NullLogger<UserProfileService>.Instance),
            new AnnotationLookupService(translationsCtx),
            indexer,
            NullLogger<ApprovalService>.Instance);

    private static IdentityDbContext NewIdentityContext(ReviewsDbContext reviewsContext)
    {
        var connectionString = reviewsContext.Database.GetConnectionString()!;
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new IdentityDbContext(options);
    }

    private async Task<string> SeedProfileAsync(Role role, CancellationToken ct)
    {
        var logtoUserId = $"logto|{Guid.NewGuid():N}";
        await using var ctx = postgres.CreateIdentityContext();
        var profile = UserProfile.Create(logtoUserId).Value!;
        profile.AssignRole(role);
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync(ct);
        return logtoUserId;
    }

    private sealed class FakeAnnotationLookup : IAnnotationLookupService
    {
        public Task<Result<AnnotationParentLocator>> GetParentLocatorAsync(Guid annotationId, CancellationToken cancellationToken) =>
            Task.FromResult(Result<AnnotationParentLocator>.Failure(
                Error.NotFound($"Annotation {annotationId} not found.")));
    }

    private sealed class FakeAnnotationApprovalIndexer : IAnnotationApprovalIndexer
    {
        public List<(Guid AnnotationId, AnnotationApprovalStatus Status)> Calls { get; } = new();

        public Task UpdateApprovalStatusAsync(Guid annotationId, AnnotationApprovalStatus status, CancellationToken cancellationToken)
        {
            Calls.Add((annotationId, status));
            return Task.CompletedTask;
        }
    }
}
