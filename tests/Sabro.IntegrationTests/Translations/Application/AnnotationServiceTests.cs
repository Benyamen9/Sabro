using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Infrastructure;

namespace Sabro.IntegrationTests.Translations.Application;

[Collection(IntegrationCollection.Name)]
public class AnnotationServiceTests
{
    private readonly PostgresFixture postgres;

    public AnnotationServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_PersistsAsVersionOneAndReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);
        var request = new CreateAnnotationRequest(seed.SegmentId, AnchorStart: 0, AnchorEnd: 5, Body: "First note.");

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(1);
        result.Value.PreviousVersionId.Should().BeNull();
        result.Value.Body.Should().Be("First note.");
        result.Value.SegmentId.Should().Be(seed.SegmentId);

        await using var read = postgres.CreateContext();
        (await read.Annotations.AnyAsync(a => a.Id == result.Value.Id, ct)).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_OnUnknownSegment_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);
        var request = new CreateAnnotationRequest(Guid.NewGuid(), AnchorStart: 0, AnchorEnd: 5, Body: "Orphan.");

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyBody_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);
        var request = new CreateAnnotationRequest(seed.SegmentId, 0, 5, Body: string.Empty);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().ContainKey("body");
    }

    [Fact]
    public async Task CreateAsync_WithInvertedAnchors_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);
        var request = new CreateAnnotationRequest(seed.SegmentId, AnchorStart: 5, AnchorEnd: 5, Body: "Zero-width.");

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task EditAsync_AppendsVersionTwoAndRetainsVersionOneInDb()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct);

        Guid v1Id;
        Guid v2Id;
        await using (var ctx = postgres.CreateContext())
        {
            var service = NewService(ctx);
            var created = await service.CreateAsync(
                new CreateAnnotationRequest(seed.SegmentId, 0, 5, "v1 body"),
                ct);
            created.IsSuccess.Should().BeTrue();
            v1Id = created.Value!.Id;

            var edited = await service.EditAsync(new EditAnnotationRequest(v1Id, "v2 body"), ct);
            edited.IsSuccess.Should().BeTrue();
            edited.Value!.Version.Should().Be(2);
            edited.Value.PreviousVersionId.Should().Be(v1Id);
            edited.Value.Body.Should().Be("v2 body");
            v2Id = edited.Value.Id;
        }

        await using var read = postgres.CreateContext();
        var rows = await read.Annotations
            .AsNoTracking()
            .Where(a => a.Id == v1Id || a.Id == v2Id)
            .OrderBy(a => a.Version)
            .ToListAsync(ct);

        rows.Should().HaveCount(2);
        rows[0].Id.Should().Be(v1Id);
        rows[0].Version.Should().Be(1);
        rows[0].PreviousVersionId.Should().BeNull();
        rows[1].Id.Should().Be(v2Id);
        rows[1].Version.Should().Be(2);
        rows[1].PreviousVersionId.Should().Be(v1Id);
    }

    [Fact]
    public async Task EditAsync_OnUnknownAnnotation_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);

        var result = await service.EditAsync(new EditAnnotationRequest(Guid.NewGuid(), "new body"), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task EditAsync_WithEmptyBody_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedAnnotationAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);

        var result = await service.EditAsync(new EditAnnotationRequest(seed.AnnotationId, string.Empty), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task GetByIdAsync_RoundTripsTheStoredAnnotation()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedAnnotationAsync(chapter: 2, verse: 3, ct, body: "Round trip body.");

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);

        var result = await service.GetByIdAsync(seed.AnnotationId, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(seed.AnnotationId);
        result.Value.SegmentId.Should().Be(seed.SegmentId);
        result.Value.Body.Should().Be("Round trip body.");
        result.Value.Version.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_OnMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);

        var result = await service.GetByIdAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task ListAsync_ReturnsMostRecentFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct);
        var marker = $"List-{Guid.NewGuid():N}";

        await using (var ctx = postgres.CreateContext())
        {
            var service = NewService(ctx);
            for (var i = 1; i <= 3; i++)
            {
                var created = await service.CreateAsync(
                    new CreateAnnotationRequest(seed.SegmentId, AnchorStart: i, AnchorEnd: i + 1, Body: $"{marker}-{i}"),
                    ct);
                created.IsSuccess.Should().BeTrue();
                await Task.Delay(2, ct);
            }
        }

        await using var read = postgres.CreateContext();
        var listService = NewService(read);
        var result = await listService.ListAsync(page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().BeGreaterThanOrEqualTo(3);
        var mine = result.Value.Items.Where(a => a.Body.StartsWith(marker)).ToList();
        mine.Should().HaveCount(3);
        mine.Select(a => a.Body).Should().BeEquivalentTo(
            new[] { $"{marker}-3", $"{marker}-2", $"{marker}-1" },
            options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task ListAsync_WithInvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx);

        var result = await service.ListAsync(page, pageSize, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    private static AnnotationService NewService(TranslationsDbContext ctx) =>
        new(
            ctx,
            new CreateAnnotationRequestValidator(),
            new EditAnnotationRequestValidator(),
            Substitute.For<ISearchIndex<AnnotationSearchDocument>>(),
            NullLogger<AnnotationService>.Instance);
}
