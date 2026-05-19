using Sabro.Translations.Application.Annotations;

namespace Sabro.IntegrationTests.Translations.Application;

[Collection(TranslationsCollection.Name)]
public class AnnotationLookupServiceTests
{
    private readonly PostgresFixture postgres;

    public AnnotationLookupServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task GetParentLocatorAsync_OnExistingAnnotation_ReturnsResolvedLocator()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = await postgres.SeedAnnotationAsync(chapter: 4, verse: 9, ct);

        await using var ctx = postgres.CreateContext();
        var service = new AnnotationLookupService(ctx);

        var result = await service.GetParentLocatorAsync(seed.AnnotationId, ct);

        result.IsSuccess.Should().BeTrue();
        var locator = result.Value!;
        locator.AnnotationId.Should().Be(seed.AnnotationId);
        locator.AnnotationVersion.Should().Be(seed.AnnotationVersion);
        locator.SegmentId.Should().Be(seed.SegmentId);
        locator.SourceId.Should().Be(seed.SourceId);
        locator.ChapterNumber.Should().Be(4);
        locator.VerseNumber.Should().Be(9);
    }

    [Fact]
    public async Task GetParentLocatorAsync_OnUnknownAnnotation_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateContext();
        var service = new AnnotationLookupService(ctx);

        var result = await service.GetParentLocatorAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetParentLocatorAsync_OnEmptyGuid_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateContext();
        var service = new AnnotationLookupService(ctx);

        var result = await service.GetParentLocatorAsync(Guid.Empty, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
