using Sabro.Identity.Application.UserProfiles;
using Sabro.Play.Application.GameResults;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The complete personal-data export for one user: everything Sabro's database
/// holds about them, in one portable JSON document (GDPR right to data
/// portability / right of access). Identity data managed by the identity
/// provider (email, password, social links) lives in Logto, not here — the
/// export says so explicitly so the document is honest about its scope.
/// </summary>
public sealed record ProfileExportDto(
    DateTimeOffset ExportedAt,
    string Scope,
    UserProfileDto Profile,
    IReadOnlyList<GameResultDto> GameResults);
