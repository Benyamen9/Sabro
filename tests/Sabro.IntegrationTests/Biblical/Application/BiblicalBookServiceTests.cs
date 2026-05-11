using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Biblical.Application.Books;
using Sabro.Biblical.Domain;

namespace Sabro.IntegrationTests.Biblical.Application;

[Collection(TranslationsCollection.Name)]
public class BiblicalBookServiceTests
{
    private const string MatthewSyriac = "ܡܬܝ";

    private readonly PostgresFixture postgres;

    public BiblicalBookServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task CreateAsync_WithValidPayload_PersistsBook()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = NewCode();

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(
            new CreateBiblicalBookRequest(code, "Matthew", Testament.New, Order: 40, SyriacName: MatthewSyriac),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(code);
        result.Value.SyriacName.Should().Be(MatthewSyriac);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = NewCode();

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewService(ctx);
        var first = await service.CreateAsync(
            new CreateBiblicalBookRequest(code, "Matthew", Testament.New, 40),
            ct);
        first.IsSuccess.Should().BeTrue();

        var second = await service.CreateAsync(
            new CreateBiblicalBookRequest(code, "Other Name", Testament.New, 41),
            ct);

        second.IsSuccess.Should().BeFalse();
        second.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetByCodeAsync_NormalizesInputToUpper()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = NewCode();

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewService(ctx);
        await service.CreateAsync(new CreateBiblicalBookRequest(code, "Matthew", Testament.New, 40), ct);

        var result = await service.GetByCodeAsync(code.ToLowerInvariant(), ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(code);
    }

    [Fact]
    public async Task ListAsync_OrdersByOrderThenCode()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = NewCode();

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewService(ctx);
        await service.CreateAsync(new CreateBiblicalBookRequest($"{prefix}A", "Book A", Testament.New, 50), ct);
        await service.CreateAsync(new CreateBiblicalBookRequest($"{prefix}B", "Book B", Testament.New, 49), ct);

        var result = await service.ListAsync(page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        var ordered = result.Value!.Items
            .Where(b => b.Code.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();
        ordered.Should().HaveCount(2);
        ordered[0].Code.Should().Be($"{prefix}B");
        ordered[1].Code.Should().Be($"{prefix}A");
    }

    private static BiblicalBookService NewService(Sabro.Biblical.Infrastructure.BiblicalDbContext ctx) =>
        new(ctx, new CreateBiblicalBookRequestValidator(), NullLogger<BiblicalBookService>.Instance);

    private static string NewCode()
    {
        var n = Random.Shared.Next(0, 1_000_000);
        return $"T{n:D6}";
    }
}
