using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Reviews.Application.Approvals;
using Sabro.Reviews.Domain;
using Sabro.Translations.Application.Annotations;

namespace Sabro.IntegrationTests.Reviews.Application;

[Collection(TranslationsCollection.Name)]
public class AnnotationApprovalRepublisherTests
{
    private const string Owner = "logto|owner-republish-tests";

    private readonly PostgresFixture postgres;

    public AnnotationApprovalRepublisherTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved, AnnotationApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected, AnnotationApprovalStatus.Rejected)]
    public async Task RepublishAsync_FansOutSingleApproval(
        ApprovalStatus reviewsStatus,
        AnnotationApprovalStatus expectedIndexerStatus)
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearApprovalsAsync(ct);
        var annotationId = await SeedAnnotationApprovalAsync(reviewsStatus, ct);
        var indexer = new RecordingIndexer();
        var republisher = NewRepublisher(indexer);

        var count = await republisher.RepublishAsync(ct);

        count.Should().Be(1);
        indexer.Calls.Should().ContainSingle()
            .Which.Should().Be((annotationId, expectedIndexerStatus));
    }

    [Fact]
    public async Task RepublishAsync_OnMultipleApprovalsPerAnnotation_FansOutLatestOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearApprovalsAsync(ct);
        var annotationId = Guid.NewGuid();

        await using (var ctx = postgres.CreateReviewsContext())
        {
            var first = Approval.CreateAnnotation(
                annotationId,
                version: 1,
                sourceId: Guid.NewGuid(),
                chapterNumber: 1,
                verseNumber: 1,
                ApprovalStatus.Approved,
                Owner).Value!;
            ctx.Approvals.Add(first);
            await ctx.SaveChangesAsync(ct);
        }

        await Task.Delay(20, ct);

        await using (var ctx = postgres.CreateReviewsContext())
        {
            var second = Approval.CreateAnnotation(
                annotationId,
                version: 1,
                sourceId: Guid.NewGuid(),
                chapterNumber: 1,
                verseNumber: 1,
                ApprovalStatus.Rejected,
                Owner).Value!;
            ctx.Approvals.Add(second);
            await ctx.SaveChangesAsync(ct);
        }

        var indexer = new RecordingIndexer();
        var republisher = NewRepublisher(indexer);

        var count = await republisher.RepublishAsync(ct);

        count.Should().Be(1);
        indexer.Calls.Should().ContainSingle()
            .Which.Should().Be((annotationId, AnnotationApprovalStatus.Rejected));
    }

    [Fact]
    public async Task RepublishAsync_IgnoresSegmentAndChapterApprovals()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearApprovalsAsync(ct);

        await using (var ctx = postgres.CreateReviewsContext())
        {
            var segment = Approval.CreateSegment(
                sourceId: Guid.NewGuid(),
                chapterNumber: 1,
                verseNumber: 1,
                version: 1,
                ApprovalStatus.Approved,
                Owner).Value!;
            var chapter = Approval.CreateChapter(
                sourceId: Guid.NewGuid(),
                chapterNumber: 1,
                ApprovalStatus.Approved,
                Owner).Value!;
            ctx.Approvals.AddRange(segment, chapter);
            await ctx.SaveChangesAsync(ct);
        }

        var indexer = new RecordingIndexer();
        var republisher = NewRepublisher(indexer);

        var count = await republisher.RepublishAsync(ct);

        count.Should().Be(0);
        indexer.Calls.Should().BeEmpty();
    }

    private async Task ClearApprovalsAsync(CancellationToken ct)
    {
        await using var ctx = postgres.CreateReviewsContext();
        ctx.Approvals.RemoveRange(ctx.Approvals);
        await ctx.SaveChangesAsync(ct);
    }

    private async Task<Guid> SeedAnnotationApprovalAsync(ApprovalStatus status, CancellationToken ct)
    {
        var annotationId = Guid.NewGuid();
        var approval = Approval.CreateAnnotation(
            annotationId,
            version: 1,
            sourceId: Guid.NewGuid(),
            chapterNumber: 1,
            verseNumber: 1,
            status,
            Owner).Value!;

        await using var ctx = postgres.CreateReviewsContext();
        ctx.Approvals.Add(approval);
        await ctx.SaveChangesAsync(ct);
        return annotationId;
    }

    private AnnotationApprovalRepublisher NewRepublisher(IAnnotationApprovalIndexer indexer) =>
        new(
            postgres.CreateReviewsContext(),
            indexer,
            NullLogger<AnnotationApprovalRepublisher>.Instance);

    private sealed class RecordingIndexer : IAnnotationApprovalIndexer
    {
        public List<(Guid AnnotationId, AnnotationApprovalStatus Status)> Calls { get; } = new();

        public Task UpdateApprovalStatusAsync(Guid annotationId, AnnotationApprovalStatus status, CancellationToken cancellationToken)
        {
            Calls.Add((annotationId, status));
            return Task.CompletedTask;
        }
    }
}
