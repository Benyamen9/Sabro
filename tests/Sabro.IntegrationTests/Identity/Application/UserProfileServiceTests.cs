using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Shared.Localization;

namespace Sabro.IntegrationTests.Identity.Application;

[Collection(IntegrationCollection.Name)]
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
        result.Value.PreferredScriptVariant.Should().Be(ScriptVariant.Serto);
        result.Value.Role.Should().Be(Role.Reader);
    }

    [Fact]
    public async Task AssignRole_ServerSideMutation_PersistsAcrossReload()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using (var seed = postgres.CreateIdentityContext())
        {
            var seedService = NewService(seed);
            await seedService.GetOrCreateForLogtoUserAsync(logtoUserId, ct);

            var profile = await seed.UserProfiles
                .FirstAsync(p => p.LogtoUserId == logtoUserId, ct);
            var error = profile.AssignRole(Role.Owner);
            error.Should().BeNull();
            await seed.SaveChangesAsync(ct);
        }

        await using var ctx = postgres.CreateIdentityContext();
        var service = NewService(ctx);
        var result = await service.GetOrCreateForLogtoUserAsync(logtoUserId, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(Role.Owner);
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

    [Fact]
    public async Task DeleteAsync_ExistingProfile_RemovesItAndReportsTrue()
    {
        var ct = TestContext.Current.CancellationToken;
        var logtoUserId = NewLogtoUserId();

        await using (var seed = postgres.CreateIdentityContext())
        {
            await NewService(seed).GetOrCreateForLogtoUserAsync(logtoUserId, ct);
        }

        await using var ctx = postgres.CreateIdentityContext();
        var result = await NewService(ctx).DeleteAsync(logtoUserId, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        await using var read = postgres.CreateIdentityContext();
        var exists = await read.UserProfiles.AnyAsync(p => p.LogtoUserId == logtoUserId, ct);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NoProfile_SucceedsWithFalse()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateIdentityContext();
        var result = await NewService(ctx).DeleteAsync(NewLogtoUserId(), ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    private static UserProfileService NewService(Sabro.Identity.Infrastructure.IdentityDbContext ctx) =>
        new(
            ctx,
            new UpdateUserProfileRequestValidator(Options.Create(new SupportedLanguagesOptions())),
            NullLogger<UserProfileService>.Instance);

    private static string NewLogtoUserId() => $"logto|{Guid.NewGuid():N}";
}
