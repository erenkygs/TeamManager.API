using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public() => Ok("Public OK");

    [Authorize]
    [HttpGet("private")]
    public IActionResult Private() => Ok("Private OK (token works)");
}
