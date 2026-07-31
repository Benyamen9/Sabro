using Sabro.Identity.Domain;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The role to grant, e.g. <c>{ "role": "ShmoEditor" }</c>. Serialized by name
/// rather than by number (the API uses a string enum converter), so the request
/// stays readable and survives enum reordering.
/// </summary>
public sealed record AssignRoleRequest(Role Role);
