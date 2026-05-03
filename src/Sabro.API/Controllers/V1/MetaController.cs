using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Sabro.API.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/meta")]
public sealed class MetaController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMeta() => Ok(new
    {
        name = "Sabro API",
        version = "v1",
    });
}
