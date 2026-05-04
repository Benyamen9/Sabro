using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Translations.Application.Segments;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Translations.Application;

[Collection(TranslationsCollection.Name)]
public class SegmentServiceTests
{
    private readonly PostgresFixture fixture;

    public SegmentServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_PersistsAsVersionOneAndReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sourceId, textVersionId) = await SeedSourceAndTextVersionAsync(ct);

        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var request = new CreateSegmentRequest(sourceId, ChapterNumber: 1, VerseNumber: 1, textVersionId, Content: "In the beginning.");

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(1);
        result.Value.PreviousVersionId.Should().BeNull();
        result.Value.Content.Should().Be("In the beginning.");
        result.Value.SourceId.Should().Be(sourceId);

        await using var read = fixture.CreateContext();
        var loaded = await read.Segments.FirstOrDefaultAsync(s => s.Id == result.Value.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyContent_ReturnsValidationFailureWithStructuredFieldInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sourceId, textVersionId) = await SeedSourceAndTextVersionAsync(ct);

        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var beforeCount = await ctx.Segments.CountAsync(ct);
        var request = new CreateSegmentRequest(sourceId, 1, 1, textVersionId, Content: string.Empty);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("content");

        await using var read = fixture.CreateContext();
        var afterCount = await read.Segments.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task EditAsync_OnExistingSegment_PersistsNextVersionAlongsidePredecessor()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sourceId, textVersionId) = await SeedSourceAndTextVersionAsync(ct);

        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var created = await service.CreateAsync(
            new CreateSegmentRequest(sourceId, 4, 2, textVersionId, "Original."),
            ct);
        created.IsSuccess.Should().BeTrue();

        var edited = await service.EditAsync(
            new EditSegmentRequest(created.Value!.Id, "Revised."),
            ct);

        edited.IsSuccess.Should().BeTrue();
        edited.Value!.Version.Should().Be(2);
        edited.Value.PreviousVersionId.Should().Be(created.Value.Id);
        edited.Value.Content.Should().Be("Revised.");
        edited.Value.Id.Should().NotBe(created.Value.Id);

        await using var read = fixture.CreateContext();
        var rows = await read.Segments
            .Where(s => s.SourceId == sourceId && s.ChapterNumber == 4 && s.VerseNumber == 2)
            .OrderBy(s => s.Version)
            .ToListAsync(ct);
        rows.Should().HaveCount(2);
        rows[0].Content.Should().Be("Original.");
        rows[1].Content.Should().Be("Revised.");
        rows[1].PreviousVersionId.Should().Be(rows[0].Id);
    }

    [Fact]
    public async Task EditAsync_OnMissingSegment_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);

        var result = await service.EditAsync(new EditSegmentRequest(Guid.NewGuid(), "Anything."), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task EditAsync_WithEmptyNewContent_ReturnsValidationFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);

        var result = await service.EditAsync(new EditSegmentRequest(Guid.NewGuid(), string.Empty), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("newContent");
    }

    private static SegmentService NewService(Sabro.Translations.Infrastructure.TranslationsDbContext ctx) =>
        new(
            ctx,
            new CreateSegmentRequestValidator(),
            new EditSegmentRequestValidator(),
            NullLogger<SegmentService>.Instance);

    private static string RandomLetterCode() =>
        new string(new[]
        {
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
        });

    private async Task<(Guid SourceId, Guid TextVersionId)> SeedSourceAndTextVersionAsync(CancellationToken ct)
    {
        var author = Author.Create($"Segment-Test Author {Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, "Some Source").Value!;
        var code = RandomLetterCode();
        var textVersion = TextVersion.Create(code, "ForSegmentTest", isRightToLeft: false).Value!;

        await using var seed = fixture.CreateContext();
        seed.Authors.Add(author);
        seed.Sources.Add(source);
        seed.TextVersions.Add(textVersion);
        await seed.SaveChangesAsync(ct);

        return (source.Id, textVersion.Id);
    }
}
