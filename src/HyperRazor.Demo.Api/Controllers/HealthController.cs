using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            utcTimestamp = DateTimeOffset.UtcNow
        });
    }
}
