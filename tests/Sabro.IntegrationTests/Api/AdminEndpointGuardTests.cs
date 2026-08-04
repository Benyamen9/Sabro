using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Sabro.API.Configuration;

namespace Sabro.IntegrationTests.Api;

/// <summary>
/// Every admin action must have a <b>second</b> lock.
/// </summary>
/// <remarks>
/// <para>
/// The <c>api:v1:admin</c> scope from Logto is one key that opens the outer door to
/// every admin controller at once. It answers "is this person staff", not "may they
/// touch this". Everything after it depends on each individual action checking
/// something further — an area policy, or an Owner check in its own code.
/// </para>
/// <para>
/// That invariant held when this test was written, but nothing enforced it: adding
/// an endpoint and forgetting the second check would silently hand every
/// scope-holder whatever it does, and no existing test would notice. This is the
/// thing that notices.
/// </para>
/// <para>
/// Two admin controllers enforce Owner in <i>code</i> rather than by attribute
/// (<c>UserRoleService.AuthoriseAsync</c> and <c>EnsureOwnerAsync</c>). Those are
/// listed explicitly below, so they are a deliberate, documented exception rather
/// than an oversight — and a new action on those controllers still fails this test
/// until somebody adds it to the list and says why.
/// </para>
/// </remarks>
public class AdminEndpointGuardTests
{
    /// <summary>Policies that narrow the admin scope to one area.</summary>
    private static readonly HashSet<string> AreaPolicies = new(StringComparer.Ordinal)
    {
        AuthPolicies.LexiconView,
        AuthPolicies.LexiconEdit,
        AuthPolicies.FiguresView,
        AuthPolicies.FiguresEdit,
        AuthPolicies.ChantsView,
        AuthPolicies.ChantsEdit,
    };

    /// <summary>
    /// Actions whose second lock lives in code, with the reason. Adding to this list
    /// is a decision: it means the author has checked that the action refuses a
    /// caller holding only the scope.
    /// </summary>
    private static readonly Dictionary<string, string> CodeGatedActions = new(StringComparer.Ordinal)
    {
        ["AdminPeopleController.List"] = "UserRoleService.AuthoriseAsync -> RolePermissions.CanAssignRoles (Owner)",
        ["AdminPeopleController.AssignRole"] = "UserRoleService.AuthoriseAsync -> RolePermissions.CanAssignRoles (Owner)",
        ["AdminPeopleController.SetAreaAccess"] = "UserRoleService.AuthoriseAsync -> RolePermissions.CanAssignRoles (Owner)",
        ["AdminSearchController.Rebuild"] = "EnsureOwnerAsync at the top of the action (Owner)",
        ["AdminSearchController.RepublishAnnotationApprovals"] = "EnsureOwnerAsync at the top of the action (Owner)",
    };

    [Fact]
    public void EveryAdminActionHasASecondLock()
    {
        var unguarded = new List<string>();

        foreach (var (controller, action) in AdminActions())
        {
            var key = $"{controller.Name}.{action.Name}";

            var hasAreaPolicy = action
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .Any(a => a.Policy is not null && AreaPolicies.Contains(a.Policy));

            if (hasAreaPolicy || CodeGatedActions.ContainsKey(key))
            {
                continue;
            }

            unguarded.Add(key);
        }

        unguarded.Should().BeEmpty(
            "every admin action must narrow the api:v1:admin scope with an area policy, "
            + "or enforce Owner in its own code and be listed in CodeGatedActions with the reason. "
            + "An action reachable on the scope alone is available to every staff account, "
            + "whatever their area grants say.");
    }

    [Fact]
    public void TheAuditActuallyFoundSomeEndpoints()
    {
        // Guards the guard. If the discovery below ever stops matching — a namespace
        // move, a routing change — the test above would pass by examining nothing and
        // read as "all admin endpoints are safe".
        AdminActions().Should().HaveCountGreaterThan(15);
    }

    [Fact]
    public void EveryCodeGatedEntryStillPointsAtARealAction()
    {
        // Keeps the exception list honest: a renamed or deleted action must not leave
        // a stale entry behind that would silently excuse a future action of the same
        // name.
        var actual = AdminActions()
            .Select(pair => $"{pair.Controller.Name}.{pair.Action.Name}")
            .ToHashSet(StringComparer.Ordinal);

        CodeGatedActions.Keys.Where(key => !actual.Contains(key))
            .Should().BeEmpty("CodeGatedActions must not name actions that no longer exist");
    }

    /// <summary>
    /// Every action on every controller routed under <c>/admin/</c>. Routing is the
    /// discriminator rather than a class-level attribute, because AdminSearchController
    /// carries no class-level admin policy — it is admin by route and Owner-gated in
    /// code, and a check keyed on the attribute would skip it entirely.
    /// </summary>
    private static List<(Type Controller, MethodInfo Action)> AdminActions()
    {
        var assembly = typeof(AuthPolicies).Assembly;

        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttributes<RouteAttribute>(inherit: true)
                .Any(r => r.Template.Contains("/admin/", StringComparison.OrdinalIgnoreCase)))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .Select(m => (Controller: t, Action: m)))
            .ToList();
    }
}
