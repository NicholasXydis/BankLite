using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("fixed")]
    [Tags("User")]
    public class UserController : BaseController
    {
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserService _userService;

        public UserController(IUserService userService, IValidator<ChangePasswordDto> changePasswordValidator,
            IWebHostEnvironment environment)
        {
            _userService = userService;
            _changePasswordValidator = changePasswordValidator;
            _environment = environment;
        }

        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get profile",
            Description = "Returns the authenticated user's profile information.")]
        [ProducesResponseType(typeof(UserProfileDto), 200)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> GetProfile()
        {
            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            UserProfileDto profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [HttpPost("change-password")]
        [EnableRateLimiting("changepassword")]
        [SwaggerOperation(Summary = "Change password", Description = "Changes the authenticated user's password.")]
        [ProducesResponseType(typeof(MessageResponseDto), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationFailure>), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            ValidationResult? validation = await _changePasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            await _userService.ChangePasswordAsync(userId, dto);
            return Ok(new MessageResponseDto { Message = "Password changed successfully" });
        }

        [HttpDelete("delete-account")]
        [SwaggerOperation(Summary = "Delete account",
            Description = "Permanently deletes the authenticated user's account and all associated data.")]
        [ProducesResponseType(typeof(MessageResponseDto), 200)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> DeleteAccount()
        {
            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            await _userService.DeleteAccountAsync(userId);

            Response.Cookies.Delete("accessToken", AuthCookieDeleteOptions("/"));
            Response.Cookies.Delete("refreshToken", AuthCookieDeleteOptions("/api/auth/refresh"));
            return Ok(new MessageResponseDto { Message = "Account deleted successfully" });
        }

        private CookieOptions AuthCookieDeleteOptions(string path)
        {
            bool secure = UseSecureCookies();
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
                Path = path
            };
        }

        private bool UseSecureCookies()
        {
            return !(_environment.IsDevelopment() || _environment.IsEnvironment("Testing")) || Request.IsHttps;
        }
    }
}
