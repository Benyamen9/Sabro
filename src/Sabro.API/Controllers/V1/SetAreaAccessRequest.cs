using Sabro.Identity.Domain;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The access to grant in one area, e.g. <c>{ "access": "Reviewer" }</c>.
/// </summary>
/// <remarks>
/// A null <see cref="Access"/> revokes the grant. Null rather than a "None" value
/// on purpose: no-access is the absence of a permission row, and a second way to
/// spell it would eventually disagree with the first.
/// </remarks>
public sealed record SetAreaAccessRequest(AreaAccess? Access);
