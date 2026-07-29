using BankLite.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankLite.API.Controllers
{
    [Produces("application/json")]
    public class BaseController : ControllerBase
    {
        protected IActionResult? TryGetUserId(out Guid userId)
        {
            string? claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claim == null || !Guid.TryParse(claim, out userId))
            {
                userId = Guid.Empty;
                return Unauthorized(new ErrorResponseDto { Message = "Invalid or missing user claim." });
            }

            return null;
        }

        protected CookieOptions CreateAuthCookieOptions(string path, DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = UseSecureCookies(),
                SameSite = SameSiteMode.Lax,
                Path = path,
                Expires = expires
            };
        }

        private bool UseSecureCookies()
        {
            IWebHostEnvironment environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            return !(environment.IsDevelopment() || environment.IsEnvironment("Testing")) || Request.IsHttps;
        }
    }
}