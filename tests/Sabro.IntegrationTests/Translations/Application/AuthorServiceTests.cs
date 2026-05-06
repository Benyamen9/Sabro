using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Translations.Application.Authors;

namespace Sabro.IntegrationTests.Translations.Application;

[Collection(TranslationsCollection.Name)]
public class AuthorServiceTests
{
    private readonly PostgresFixture fixture;

    public AuthorServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_PersistsAndReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var request = new CreateAuthorRequest("Service Author", "ܛܣܛܐ", "A title");

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Service Author");
        result.Value.SyriacName.Should().Be("ܛܣܛܐ");
        result.Value.Title.Should().Be("A title");
        result.Value.Id.Should().NotBe(Guid.Empty);

        await using var read = fixture.CreateContext();
        var loaded = await read.Authors.FirstOrDefaultAsync(a => a.Id == result.Value.Id, ct);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Service Author");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ReturnsValidationFailureWithStructuredFieldInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var beforeCount = await ctx.Authors.CountAsync(ct);
        var request = new CreateAuthorRequest(Name: string.Empty, SyriacName: null, Title: null);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("name");
        result.Error.Fields["name"].Should().NotBeEmpty();

        await using var read = fixture.CreateContext();
        var afterCount = await read.Authors.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task CreateAsync_WithLatinSyriacName_FailsAtDomainLayerAndDoesNotPersist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var beforeCount = await ctx.Authors.CountAsync(ct);
        var request = new CreateAuthorRequest(Name: "Author", SyriacName: "Latin", Title: null);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");

        // Domain failures don't carry per-field info — they're whole-aggregate invariants
        result.Error.Fields.Should().BeNull();

        await using var read = fixture.CreateContext();
        var afterCount = await read.Authors.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task GetByIdAsync_OnExistingAuthor_ReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var created = await service.CreateAsync(new CreateAuthorRequest("GetById Author", null, null), ct);

        var fetched = await service.GetByIdAsync(created.Value!.Id, ct);

        fetched.IsSuccess.Should().BeTrue();
        fetched.Value!.Id.Should().Be(created.Value.Id);
        fetched.Value.Name.Should().Be("GetById Author");
    }

    [Fact]
    public async Task GetByIdAsync_OnMissingAuthor_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);

        var result = await service.GetByIdAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task ListAsync_ReturnsSeededRowsNewestFirstWithEchoedPageMetadata()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = $"List-Test-{Guid.NewGuid():N}-";

        await using (var ctx = fixture.CreateContext())
        {
            var service = NewService(ctx);
            for (var i = 1; i <= 3; i++)
            {
                var created = await service.CreateAsync(new CreateAuthorRequest($"{prefix}{i:000}", null, null), ct);
                created.IsSuccess.Should().BeTrue();
                await Task.Delay(2, ct);
            }
        }

        await using var read = fixture.CreateContext();
        var listService = NewService(read);
        var result = await listService.ListAsync(page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(200);
        result.Value.Total.Should().BeGreaterThanOrEqualTo(3);

        var mine = result.Value.Items.Where(a => a.Name.StartsWith(prefix)).ToList();
        mine.Should().HaveCount(3);
        mine.Select(a => a.Name).Should().BeEquivalentTo(
            new[] { $"{prefix}003", $"{prefix}002", $"{prefix}001" },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ListAsync_RespectsPageSize()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);

        var result = await service.ListAsync(page: 1, pageSize: 2, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(2);
        result.Value.Items.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task ListAsync_WithInvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);

        var result = await service.ListAsync(page, pageSize, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
    }

    private static AuthorService NewService(Sabro.Translations.Infrastructure.TranslationsDbContext ctx) =>
        new(ctx, new CreateAuthorRequestValidator(), NullLogger<AuthorService>.Instance);
}
