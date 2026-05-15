using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterUserDto> _registerValidator;
        private readonly IValidator<LoginUserDto> _loginValidator;

        public AuthController(IAuthService authService, IValidator<RegisterUserDto> registerValidator, IValidator<LoginUserDto> loginValidator)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
        }

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var validation = await _registerValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var (token, refreshToken, result) = await _authService.RegisterAsync(dto);
            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(60)
            });
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                Path = "/api/auth/refresh"
            });
            return Ok(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            var validation = await _loginValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var (token, refreshToken, result) = await _authService.LoginAsync(dto);
            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(60)
            });
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                Path = "/api/auth/refresh"
            });
            return Ok(result);
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "No refresh token provided." });

            var (token, newRefreshToken, result) = await _authService.RefreshAsync(refreshToken);
            Response.Cookies.Append("accessToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(60)
            });
            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                Path = "/api/auth/refresh"
            });
            return Ok(result);
        }

        [HttpPost("logout")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshToken);

            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok();
        }
    }
}
