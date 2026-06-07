using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult FromError(Error error) => error.Code switch
    {
        "validation" when error.Fields is { Count: > 0 } => ValidationProblem(new ValidationProblemDetails(
            error.Fields.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()))),
        "validation" => ValidationProblem(detail: error.Message),
        "not_found" => Problem(detail: error.Message, statusCode: StatusCodes.Status404NotFound, title: "Not Found"),
        "conflict" => Problem(detail: error.Message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        "forbidden" => Problem(detail: error.Message, statusCode: StatusCodes.Status403Forbidden, title: "Forbidden"),
        _ => Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError),
    };

    /// <summary>
    /// Reads the OIDC <c>sub</c> claim from the validated JWT. ASP.NET Core's JWT
    /// bearer handler maps <c>sub</c> to <see cref="ClaimTypes.NameIdentifier"/> by
    /// default; we also fall back to the raw <c>sub</c> name for handlers that
    /// disable inbound claim mapping.
    /// </summary>
    protected Result<string> ResolveLogtoUserId()
    {
        var logtoUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return Result<string>.Failure(Error.Validation("Authenticated user is missing a sub claim."));
        }

        return Result<string>.Success(logtoUserId);
    }
}
