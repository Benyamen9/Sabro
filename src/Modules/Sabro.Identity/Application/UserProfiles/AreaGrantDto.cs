using Sabro.Identity.Domain;

namespace Sabro.Identity.Application.UserProfiles;

/// <summary>One person's access to one content area, as carried on the wire.</summary>
public sealed record AreaGrantDto(ContentArea Area, AreaAccess Access);
