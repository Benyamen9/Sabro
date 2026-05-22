using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sabro.Biblical.Application.Books;
using Sabro.Biblical.Application.Passages;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Biblical.Application;

[Collection(IntegrationCollection.Name)]
public class BiblicalPassageServiceTests
{
    private readonly PostgresFixture postgres;

    public BiblicalPassageServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task GetOrCreateAsync_OnFirstCall_CreatesPassageAndReturnsWasCreatedTrue()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = await SeedBookAsync(ct);

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewPassageService(ctx);

        var result = await service.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(code, 5, 3), ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasCreated.Should().BeTrue();
        result.Value.Passage.BookCode.Should().Be(code);
        result.Value.Passage.ChapterNumber.Should().Be(5);
        result.Value.Passage.VerseNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetOrCreateAsync_OnSecondCall_ReturnsExistingPassageWithWasCreatedFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = await SeedBookAsync(ct);

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewPassageService(ctx);
        var first = await service.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(code, 1, 1), ct);
        var second = await service.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(code, 1, 1), ct);

        first.Value!.WasCreated.Should().BeTrue();
        second.Value!.WasCreated.Should().BeFalse();
        second.Value.Passage.Id.Should().Be(first.Value.Passage.Id);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithUnknownBook_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewPassageService(ctx);

        var result = await service.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest("ZZZNOPE", 1, 1), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetOrCreateAsync_NormalizesBookCodeToUpper()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = await SeedBookAsync(ct);

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewPassageService(ctx);

        var result = await service.GetOrCreateAsync(
            new GetOrCreateBiblicalPassageRequest(code.ToLowerInvariant(), 2, 2),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Passage.BookCode.Should().Be(code);
    }

    [Fact]
    public async Task ListAsync_FiltersByBookCodeAndChapter()
    {
        var ct = TestContext.Current.CancellationToken;
        var codeA = await SeedBookAsync(ct);
        var codeB = await SeedBookAsync(ct);

        await using (var seedCtx = postgres.CreateBiblicalContext())
        {
            var seedService = NewPassageService(seedCtx);
            await seedService.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(codeA, 7, 1), ct);
            await seedService.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(codeA, 7, 2), ct);
            await seedService.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(codeA, 8, 1), ct);
            await seedService.GetOrCreateAsync(new GetOrCreateBiblicalPassageRequest(codeB, 7, 1), ct);
        }

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewPassageService(ctx);

        var result = await service.ListAsync(codeA, chapterNumber: 7, page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().OnlyContain(p => p.BookCode == codeA && p.ChapterNumber == 7);
    }

    private static BiblicalPassageService NewPassageService(Sabro.Biblical.Infrastructure.BiblicalDbContext ctx) =>
        new(
            ctx,
            new GetOrCreateBiblicalPassageRequestValidator(),
            Substitute.For<ISearchIndex<BiblicalPassageSearchDocument>>(),
            NullLogger<BiblicalPassageService>.Instance);

    private static BiblicalBookService NewBookService(Sabro.Biblical.Infrastructure.BiblicalDbContext ctx) =>
        new(ctx, new CreateBiblicalBookRequestValidator(), NullLogger<BiblicalBookService>.Instance);

    private static string NewCode()
    {
        var n = Random.Shared.Next(0, 1_000_000);
        return $"P{n:D6}";
    }

    private async Task<string> SeedBookAsync(CancellationToken ct)
    {
        var code = NewCode();
        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewBookService(ctx);
        var result = await service.CreateAsync(
            new CreateBiblicalBookRequest(code, $"Seed Book {code}", Testament.New, Order: Random.Shared.Next(1, 100)),
            ct);
        result.IsSuccess.Should().BeTrue();
        return code;
    }
}
