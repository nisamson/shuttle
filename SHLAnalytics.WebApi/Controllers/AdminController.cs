using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace SHLAnalytics.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase {
    private readonly ILogger<AdminController> logger;

    public AdminController(ILogger<AdminController> logger) {
        this.logger = logger;
    }

    [HttpGet("commit-hash")]
    public ActionResult<string> GetCommitHash() {
        return ThisAssembly.GitCommitId;
    }
}
