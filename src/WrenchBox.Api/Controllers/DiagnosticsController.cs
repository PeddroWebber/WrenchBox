using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WrenchBox.Api.Controllers;

[ApiController]
[Route("api/v1/diagnostics")]
[AllowAnonymous]
public class DiagnosticsController : ControllerBase
{
    [HttpGet("load")]
    public IActionResult Load([FromQuery] int iterations = 750_000)
    {
        iterations = Math.Clamp(iterations, 1, 5_000_000);
        var acc = 0d;
        for (var i = 0; i < iterations; i++)
            acc += Math.Sqrt(i);

        return Ok(new { iterations, result = acc });
    }
}
