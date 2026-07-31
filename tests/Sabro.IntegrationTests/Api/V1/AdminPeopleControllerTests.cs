using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Sabro.API.Controllers.V1;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Role management over HTTP: who may read the list, who may grant, and the two
/// guards that keep the installation recoverable — the no-Owner bootstrap and the
/// refusal to change your own role.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AdminPeopleControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;

    public AdminPeopleControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
    }

    [Fact]
    public async Task Get_WithNoOwnerAnywhere_IsAllowedSoAnOwnerCanBeAppointed()
    {
        // The bootstrap clause. Without it, granting Owner requires being Owner
        // and the only escape is editing the database by hand.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var client = ClientFor("bootstrap-caller");
        await EnsureProfileAsync(client, ct);

        var response = await client.GetAsync("/api/v1/admin/people", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_WithNoOwnerAnywhere_CanAppointOne()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var caller = ClientFor("bootstrap-appointer");
        await EnsureProfileAsync(caller, ct);
        var targetId = await CreateProfileAsync("first-owner", ct);

        var response = await AssignAsync(caller, targetId, Role.Owner, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RoleOfAsync(targetId, ct)).Should().Be(Role.Owner);
    }

    [Fact]
    public async Task Get_AsNonOwnerOnceAnOwnerExists_Returns403()
    {
        // The bootstrap clause must close itself the moment an Owner exists.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        await CreateProfileAsync("the-owner", ct, Role.Owner);
        using var client = ClientFor("a-mere-editor");
        await EnsureProfileAsync(client, ct);
        await SetRoleAsync("a-mere-editor", Role.ShmoEditor, ct);

        var response = await client.GetAsync("/api/v1/admin/people", ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_AsShmoEditor_Returns403()
    {
        // Holding the admin scope is not enough: an area editor must not be able
        // to promote themselves or anyone else.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        await CreateProfileAsync("the-owner", ct, Role.Owner);
        using var client = ClientFor("sneaky-editor");
        await EnsureProfileAsync(client, ct);
        await SetRoleAsync("sneaky-editor", Role.ShmoEditor, ct);
        var targetId = await CreateProfileAsync("some-reader", ct);

        var response = await AssignAsync(client, targetId, Role.Owner, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await RoleOfAsync(targetId, ct)).Should().Be(Role.Reader);
    }

    [Fact]
    public async Task Put_AsOwner_GrantsTheRole()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var owner = ClientFor("granting-owner");
        await EnsureProfileAsync(owner, ct);
        await SetRoleAsync("granting-owner", Role.Owner, ct);
        var targetId = await CreateProfileAsync("the-helper", ct);

        var response = await AssignAsync(owner, targetId, Role.ShmoEditor, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Role.Should().Be(Role.ShmoEditor);
        (await RoleOfAsync(targetId, ct)).Should().Be(Role.ShmoEditor);
    }

    [Fact]
    public async Task Put_OnYourOwnProfile_IsRefused()
    {
        // The sole Owner demoting themselves leaves nobody able to grant roles.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var owner = ClientFor("self-demoter");
        await EnsureProfileAsync(owner, ct);
        await SetRoleAsync("self-demoter", Role.Owner, ct);
        var ownId = await ProfileIdOfAsync("self-demoter", ct);

        var response = await AssignAsync(owner, ownId, Role.Reader, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await RoleOfAsync(ownId, ct)).Should().Be(Role.Owner, "the last Owner must not be able to strand the installation");
    }

    [Fact]
    public async Task Get_WithNoLogtoManagementConfigured_StillListsPeople()
    {
        // The integration environment has no Management API credentials, which is
        // exactly the degraded case: names come back null, but the list renders and
        // the role — the only thing authorisation depends on — is present.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var owner = ClientFor("degraded-owner");
        await EnsureProfileAsync(owner, ct);
        await SetRoleAsync("degraded-owner", Role.Owner, ct);
        await CreateProfileAsync("someone-else", ct);

        var response = await owner.GetAsync("/api/v1/admin/people", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var people = await response.Content.ReadFromJsonAsync<List<PersonDto>>(SabroApiFactory.JsonOptions, ct);
        people.Should().HaveCount(2);
        people!.Should().OnlyContain(p => p.Name == null && p.Email == null);
        people.Should().ContainSingle(p => p.Role == Role.Owner);
        people.Should().ContainSingle(p => p.IsYou);
    }

    [Fact]
    public async Task Get_DoesNotLeakTheLogtoUserId()
    {
        // The opaque Logto id is Sabro's join key, not something the browser needs.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var owner = ClientFor("privacy-owner");
        await EnsureProfileAsync(owner, ct);
        await SetRoleAsync("privacy-owner", Role.Owner, ct);

        var response = await owner.GetAsync("/api/v1/admin/people", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        body.Should().NotContain("privacy-owner");
        body.Should().NotContain("logtoUserId");
    }

    [Fact]
    public async Task Put_OnYourOwnProfile_WithNoOwnerAnywhere_AppointsYouOwner()
    {
        // Found in production on the first shipped build: every profile read
        // "can only play", and the no-self-assignment rule forbade the one person
        // who could fix it from doing so. Bootstrap has to include yourself.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var caller = ClientFor("stranded-admin");
        await EnsureProfileAsync(caller, ct);
        var ownId = await ProfileIdOfAsync("stranded-admin", ct);

        var response = await AssignAsync(caller, ownId, Role.Owner, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RoleOfAsync(ownId, ct)).Should().Be(Role.Owner);
    }

    [Fact]
    public async Task Put_OnYourOwnProfile_WithNoOwner_RefusesAnythingButOwner()
    {
        // Bootstrap exists to create an Owner, not to let anyone edit their own
        // permissions freely while none exists.
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var caller = ClientFor("opportunist");
        await EnsureProfileAsync(caller, ct);
        var ownId = await ProfileIdOfAsync("opportunist", ct);

        var response = await AssignAsync(caller, ownId, Role.LexiconEditor, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await RoleOfAsync(ownId, ct)).Should().Be(Role.Reader);
    }

    [Fact]
    public async Task Put_ForAnUnknownProfile_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearProfilesAsync(ct);
        using var owner = ClientFor("owner-404");
        await EnsureProfileAsync(owner, ct);
        await SetRoleAsync("owner-404", Role.Owner, ct);

        var response = await AssignAsync(owner, Guid.NewGuid(), Role.ShmoEditor, ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async Task<HttpResponseMessage> AssignAsync(
        HttpClient client,
        Guid profileId,
        Role role,
        CancellationToken ct) =>
        await client.PutAsJsonAsync($"/api/v1/admin/people/{profileId}/role", new AssignRoleRequest(role), ct);

    /// <summary>Touching /profile/me is what creates a profile row.</summary>
    private static async Task EnsureProfileAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync("/api/v1/profile/me", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private HttpClient ClientFor(string logtoUserId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeaderName, logtoUserId);
        return client;
    }

    private async Task<Guid> CreateProfileAsync(string logtoUserId, CancellationToken ct, Role? role = null)
    {
        using var client = ClientFor(logtoUserId);
        await EnsureProfileAsync(client, ct);
        if (role.HasValue)
        {
            await SetRoleAsync(logtoUserId, role.Value, ct);
        }

        return await ProfileIdOfAsync(logtoUserId, ct);
    }

    private async Task ClearProfilesAsync(CancellationToken ct)
    {
        await using var ctx = postgres.CreateIdentityContext();
        await ctx.UserProfiles.ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Seeds a role straight into the database. The API cannot be used for this in
    /// the arrange step — that is the very deadlock the bootstrap clause exists for.
    /// </summary>
    private async Task SetRoleAsync(string logtoUserId, Role role, CancellationToken ct)
    {
        await using var ctx = postgres.CreateIdentityContext();
        var profile = await ctx.UserProfiles.FirstAsync(p => p.LogtoUserId == logtoUserId, ct);
        profile.AssignRole(role);
        await ctx.SaveChangesAsync(ct);
    }

    private async Task<Guid> ProfileIdOfAsync(string logtoUserId, CancellationToken ct)
    {
        await using var ctx = postgres.CreateIdentityContext();
        var profile = await ctx.UserProfiles.AsNoTracking().FirstAsync(p => p.LogtoUserId == logtoUserId, ct);
        return profile.Id;
    }

    private async Task<Role> RoleOfAsync(Guid profileId, CancellationToken ct)
    {
        await using var ctx = postgres.CreateIdentityContext();
        var profile = await ctx.UserProfiles.AsNoTracking().FirstAsync(p => p.Id == profileId, ct);
        return profile.Role;
    }
}
