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
        _ => Problem(detail: error.Message, statusCode: StatusCodes.Status500InternalServerError),
    };
}
