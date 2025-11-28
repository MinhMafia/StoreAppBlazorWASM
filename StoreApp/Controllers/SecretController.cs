using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StoreApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecretController : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public IActionResult GetSecret()
        {
            var user = User.Identity?.Name ?? "unknown";
            return Ok(new { message = $"Hello {user}, this is protected data." });
        }

        [HttpGet("admin")]
        [Authorize(Roles = "admin")]
        public IActionResult AdminOnly() => Ok(new { secret = "admin data" });
    }
}
