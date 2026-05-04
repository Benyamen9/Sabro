using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Translations.Application.Sources;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Translations.Application;

[Collection(TranslationsCollection.Name)]
public class SourceServiceTests
{
    private readonly PostgresFixture fixture;

    public SourceServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_PersistsAndReturnsDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = await SeedAuthorAsync(ct);

        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var request = new CreateSourceRequest(
            AuthorId: author.Id,
            Title: "Commentary on Matthew",
            OriginalLanguageCode: "syr",
            Description: "A description.");

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AuthorId.Should().Be(author.Id);
        result.Value.Title.Should().Be("Commentary on Matthew");
        result.Value.OriginalLanguageCode.Should().Be("syr");
        result.Value.Description.Should().Be("A description.");
        result.Value.Id.Should().NotBe(Guid.Empty);

        await using var read = fixture.CreateContext();
        var loaded = await read.Sources.FirstOrDefaultAsync(s => s.Id == result.Value.Id, ct);
        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Commentary on Matthew");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTitle_ReturnsValidationFailureWithStructuredFieldInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = await SeedAuthorAsync(ct);

        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var beforeCount = await ctx.Sources.CountAsync(ct);
        var request = new CreateSourceRequest(author.Id, Title: string.Empty, OriginalLanguageCode: null, Description: null);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("title");
        result.Error.Fields["title"].Should().NotBeEmpty();

        await using var read = fixture.CreateContext();
        var afterCount = await read.Sources.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyAuthorId_ReturnsValidationFailureWithStructuredFieldInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateContext();
        var service = NewService(ctx);
        var beforeCount = await ctx.Sources.CountAsync(ct);
        var request = new CreateSourceRequest(AuthorId: Guid.Empty, Title: "A title", OriginalLanguageCode: null, Description: null);

        var result = await service.CreateAsync(request, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().NotBeNull();
        result.Error.Fields!.Should().ContainKey("authorId");

        await using var read = fixture.CreateContext();
        var afterCount = await read.Sources.CountAsync(ct);
        afterCount.Should().Be(beforeCount);
    }

    private static SourceService NewService(Sabro.Translations.Infrastructure.TranslationsDbContext ctx) =>
        new(ctx, new CreateSourceRequestValidator(), NullLogger<SourceService>.Instance);

    private async Task<Author> SeedAuthorAsync(CancellationToken ct)
    {
        var author = Author.Create($"Source-Test Author {Guid.NewGuid():N}").Value!;
        await using var seed = fixture.CreateContext();
        seed.Authors.Add(author);
        await seed.SaveChangesAsync(ct);
        return author;
    }
}
