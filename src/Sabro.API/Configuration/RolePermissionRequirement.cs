using Microsoft.AspNetCore.Authorization;
using Sabro.Identity.Domain;

namespace Sabro.API.Configuration;

/// <summary>
/// Requires the caller's Sabro role to satisfy a predicate from
/// <see cref="RolePermissions"/>.
/// </summary>
/// <remarks>
/// Complements rather than replaces the scope check. Logto's <c>api:v1:admin</c>
/// scope decides whether a request may reach an admin endpoint at all; this
/// decides which area it may touch once inside. Both must pass — the policies
/// carrying this requirement sit alongside the class-level admin policy, and
/// ASP.NET requires every applicable policy to succeed.
/// </remarks>
public sealed class RolePermissionRequirement : IAuthorizationRequirement
{
    public RolePermissionRequirement(Func<Role, bool> isAllowed, string description)
    {
        IsAllowed = isAllowed;
        Description = description;
    }

    public Func<Role, bool> IsAllowed { get; }

    /// <summary>Human-readable purpose, for logs and for reading the policy list.</summary>
    public string Description { get; }
}
