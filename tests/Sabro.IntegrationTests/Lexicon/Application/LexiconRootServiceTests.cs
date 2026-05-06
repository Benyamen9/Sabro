using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Roots;

namespace Sabro.IntegrationTests.Lexicon.Application;

[Collection(TranslationsCollection.Name)]
public class LexiconRootServiceTests
{
    private readonly PostgresFixture fixture;

    public LexiconRootServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_PersistsAndReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var request = new CreateLexiconRootRequest(RandomSyriacRoot());

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Form.Should().Be(request.Form);
        result.Value.Id.Should().NotBe(Guid.Empty);

        await using var read = fixture.CreateLexiconContext();
        var loaded = await read.Roots.FirstOrDefaultAsync(r => r.Id == result.Value.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithEmptyForm_ReturnsValidationFailureWithFieldInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(new CreateLexiconRootRequest(string.Empty), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("form");
    }

    [Fact]
    public async Task CreateAsync_WithLatinForm_FailsAtDomainLayer()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);

        var result = await service.CreateAsync(new CreateLexiconRootRequest("ktb"), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_OnExisting_ReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);
        var created = await service.CreateAsync(new CreateLexiconRootRequest(RandomSyriacRoot()), ct);

        var fetched = await service.GetByIdAsync(created.Value!.Id, ct);

        fetched.IsSuccess.Should().BeTrue();
        fetched.Value!.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_OnMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);

        var result = await service.GetByIdAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task ListAsync_ReturnsSeededRowsNewestFirstWithEchoedPageMetadata()
    {
        var ct = TestContext.Current.CancellationToken;
        var seeds = new List<string>();

        await using (var ctx = fixture.CreateLexiconContext())
        {
            var service = NewService(ctx);
            for (var i = 0; i < 3; i++)
            {
                var form = RandomSyriacRoot();
                seeds.Add(form);
                var created = await service.CreateAsync(new CreateLexiconRootRequest(form), ct);
                created.IsSuccess.Should().BeTrue();
                await Task.Delay(2, ct);
            }
        }

        await using var read = fixture.CreateLexiconContext();
        var listService = NewService(read);
        var result = await listService.ListAsync(page: 1, pageSize: 200, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(200);
        result.Value.Total.Should().BeGreaterThanOrEqualTo(3);

        var mine = result.Value.Items
            .Where(r => seeds.Contains(r.Form))
            .Select(r => r.Form)
            .ToList();
        mine.Should().HaveCount(3);
        mine.Should().BeEquivalentTo(seeds.AsEnumerable().Reverse(), options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task ListAsync_WithInvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = NewService(ctx);

        var result = await service.ListAsync(page, pageSize, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    private static LexiconRootService NewService(Sabro.Lexicon.Infrastructure.LexiconDbContext ctx) =>
        new(ctx, new CreateLexiconRootRequestValidator(), NullLogger<LexiconRootService>.Instance);

    private static string RandomSyriacRoot() =>
        new string(new[]
        {
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
        });
}
