using Microsoft.EntityFrameworkCore;
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
        var service = new AuthorService(ctx, new CreateAuthorRequestValidator());
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
    public async Task CreateAsync_WithEmptyName_ReturnsValidationFailureAndDoesNotPersist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = new AuthorService(ctx, new CreateAuthorRequestValidator());
        var beforeCount = await ctx.Authors.CountAsync(ct);
        var request = new CreateAuthorRequest(Name: string.Empty, SyriacName: null, Title: null);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");

        await using var read = fixture.CreateContext();
        var afterCount = await read.Authors.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task CreateAsync_WithLatinSyriacName_FailsAtDomainLayerAndDoesNotPersist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = new AuthorService(ctx, new CreateAuthorRequestValidator());
        var beforeCount = await ctx.Authors.CountAsync(ct);
        var request = new CreateAuthorRequest(Name: "Author", SyriacName: "Latin", Title: null);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");

        await using var read = fixture.CreateContext();
        var afterCount = await read.Authors.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }
}
