using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankLite.API.Controllers
{
    public class BaseController : ControllerBase
    {
        protected IActionResult? TryGetUserId(out Guid userId)
        {
            string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claim == null || !Guid.TryParse(claim, out userId))
            {
                userId = Guid.Empty;
                return Unauthorized(new { message = "Invalid or missing user claim." });
            }

            return null;
        }
    }
}