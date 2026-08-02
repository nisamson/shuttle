using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shuttle.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase {
    private readonly ILogger<HelloController> logger;

    public HelloController(ILogger<HelloController> logger) {
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Get() {
        logger.LogInformation("Received request to {Endpoint} at {Time}", nameof(Get), DateTime.UtcNow);
        return Ok(new { Message = "Hello, world!" });
    }
}
