using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// One person on the People page: Sabro's own profile data, plus whatever Logto
/// could tell us about who they are.
/// </summary>
/// <remarks>
/// <see cref="Name"/> and <see cref="Email"/> are read from Logto at request time
/// and are <b>not stored by Sabro</b> — <c>UserProfile</c> deliberately mirrors
/// neither. Both are null when the Management API is unconfigured or the lookup
/// failed, which is a display degradation and never an authorisation one:
/// <see cref="Role"/> and <see cref="Id"/> come from Sabro's database and are
/// always present.
/// </remarks>
public sealed record PersonDto(
    Guid Id,
    Role Role,
    IReadOnlyList<AreaGrantDto> Areas,
    string? DisplayName,
    string? Name,
    string? Email,
    DateTimeOffset CreatedAt,
    bool IsYou);
