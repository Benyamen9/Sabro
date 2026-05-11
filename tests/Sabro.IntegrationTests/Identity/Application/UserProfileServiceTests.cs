using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;

namespace Sabro.IntegrationTests.Identity.Application;

[Collection(TranslationsCollection.Name)]
public class UserProfileServiceTests
{
    private readonly PostgresFixture postgres;

    public UserProfileServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task GetOrCreate_OnFirstCall_PersistsDefaultProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);

        var result = await service.GetOrCreateForLogtoUserAsync(logtoUserId, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LogtoUserId.Should().Be(logtoUserId);
        result.Value.PreferredLanguage.Should().Be("en");
        result.Value.PreferredScriptVariant.Should().Be(ScriptVariant.Estrangela);
    }

    [Fact]
    public async Task GetOrCreate_OnSecondCall_ReturnsSameProfileIdempotently()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);
        var first = await service.GetOrCreateForLogtoUserAsync(logtoUserId, ct);
        var second = await service.GetOrCreateForLogtoUserAsync(logtoUserId, ct);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value!.Id.Should().Be(first.Value!.Id);
        second.Value.CreatedAt.Should().BeCloseTo(first.Value.CreatedAt, precision: TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task GetOrCreate_WithEmptyLogtoUserId_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);

        var result = await service.GetOrCreateForLogtoUserAsync(string.Empty, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Update_OnExistingProfile_MutatesPreferences()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using (var seed = postgres.CreateIdentityContext())
        {
            var seedService = NewService(seed);
            await seedService.GetOrCreateForLogtoUserAsync(logtoUserId, ct);
        }

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);
        var result = await service.UpdateAsync(
            logtoUserId,
            new UpdateUserProfileRequest("fr", ScriptVariant.Serto),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreferredLanguage.Should().Be("fr");
        result.Value.PreferredScriptVariant.Should().Be(ScriptVariant.Serto);
    }

    [Fact]
    public async Task Update_OnMissingProfile_CreatesItWithRequestedPreferences()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);

        var result = await service.UpdateAsync(
            logtoUserId,
            new UpdateUserProfileRequest("nl", ScriptVariant.Madnhaya),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreferredLanguage.Should().Be("nl");
        result.Value.PreferredScriptVariant.Should().Be(ScriptVariant.Madnhaya);
    }

    [Fact]
    public async Task Update_WithUnsupportedLanguage_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);

        var result = await service.UpdateAsync(
            logtoUserId,
            new UpdateUserProfileRequest("xx", ScriptVariant.Serto),
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().ContainKey("preferredLanguage");
    }

    private static UserProfileService NewService(Sabro.Identity.Infrastructure.IdentityDbContext ctx) =>
        new(
            ctx,
            new UpdateUserProfileRequestValidator(),
            NullLogger<UserProfileService>.Instance);

    private static string NewLogtoUserId() => $"logto|{Guid.NewGuid():N}";
}
