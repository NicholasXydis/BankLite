using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterUserDto> _registerValidator;
        private readonly IValidator<LoginUserDto> _loginValidator;
        private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
        private readonly IConfiguration _configuration;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;


        public AuthController(IAuthService authService, IValidator<RegisterUserDto> registerValidator, IValidator<LoginUserDto> loginValidator, IConfiguration configuration, IValidator<ForgotPasswordDto> forgotPasswordValidator, IValidator<ResetPasswordDto> resetPasswordValidator)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _configuration = configuration;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
        }

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        [SwaggerOperation(Summary = "Register a new user", Description = "Creates a new user account and returns a JWT access token via HttpOnly cookie.")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var validation = await _registerValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var (token, refreshToken, result) = await _authService.RegisterAsync(dto);
            Response.Cookies.Append("accessToken", token, AccessTokenCookieOptions());
            Response.Cookies.Append("refreshToken", refreshToken, RefreshTokenCookieOptions());
            return Ok(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        [SwaggerOperation(Summary = "Login", Description = "Authenticates a user and returns a JWT access token via HttpOnly cookie.")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            var validation = await _loginValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var (token, refreshToken, result) = await _authService.LoginAsync(dto);
            Response.Cookies.Append("accessToken", token, AccessTokenCookieOptions());
            Response.Cookies.Append("refreshToken", refreshToken, RefreshTokenCookieOptions());
            return Ok(result);
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("refresh")]
        [SwaggerOperation(Summary = "Refresh access token", Description = "Issues a new JWT access token using the HttpOnly refresh token cookie.")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "No refresh token provided." });

            var (token, newRefreshToken, result) = await _authService.RefreshAsync(refreshToken);
            Response.Cookies.Append("accessToken", token, AccessTokenCookieOptions());
            Response.Cookies.Append("refreshToken", newRefreshToken, RefreshTokenCookieOptions());
            return Ok(result);
        }


        [HttpPost("forgot-password")]
        [EnableRateLimiting("forgotpassword")]
        [SwaggerOperation(Summary = "Request password reset", Description = "Sends a password reset email if the provided email exists in the system.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {

            var validation = await _forgotPasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var resetBaseUrl = _configuration["Frontend:ResetPasswordUrl"]
            ?? throw new InvalidOperationException("Reset password URL not configured");
            await _authService.ForgotPasswordAsync(dto.Email, resetBaseUrl);
            return Ok(new { message = "If that email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("forgotpassword")]
        [SwaggerOperation(Summary = "Reset password", Description = "Resets the user's password using a valid reset token.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {

            var validation = await _resetPasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
            return Ok(new { message = "Password reset successfully." });
        }

        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logout", Description = "Revokes the refresh token and clears all auth cookies.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.RevokeRefreshTokenAsync(refreshToken);

            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            return Ok();
        }

        private CookieOptions AccessTokenCookieOptions() => new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"]!))
        };

        private static CookieOptions RefreshTokenCookieOptions() => new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(1),
            Path = "/api/auth/refresh"
        };
    }
}
