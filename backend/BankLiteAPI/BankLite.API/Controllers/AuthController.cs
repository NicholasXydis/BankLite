using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Options;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;
        private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
        private readonly FrontendSettings _frontendSettings;
        private readonly JwtSettings _jwtSettings;
        private readonly IValidator<LoginUserDto> _loginValidator;
        private readonly IValidator<RegisterUserDto> _registerValidator;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;


        public AuthController(IAuthService authService, IValidator<RegisterUserDto> registerValidator,
            IValidator<LoginUserDto> loginValidator,
            IValidator<ForgotPasswordDto> forgotPasswordValidator, IValidator<ResetPasswordDto> resetPasswordValidator,
            IWebHostEnvironment environment, IOptions<JwtSettings> jwtSettings,
            IOptions<FrontendSettings> frontendSettings)
        {
            _authService = authService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _forgotPasswordValidator = forgotPasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _environment = environment;
            _jwtSettings = jwtSettings.Value;
            _frontendSettings = frontendSettings.Value;
        }

        [HttpPost("register")]
        [EnableRateLimiting("register")]
        [SwaggerOperation(Summary = "Register a new user",
            Description = "Creates a new user account and returns a JWT access token via HttpOnly cookie.")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            ValidationResult? validation = await _registerValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            (string token, string refreshToken, AuthResponseDto result) = await _authService.RegisterAsync(dto);
            Response.Cookies.Append("accessToken", token, AccessTokenCookieOptions());
            Response.Cookies.Append("refreshToken", refreshToken, RefreshTokenCookieOptions());
            return Ok(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        [SwaggerOperation(Summary = "Login",
            Description = "Authenticates a user and returns a JWT access token via HttpOnly cookie.")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
        {
            ValidationResult? validation = await _loginValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            (string token, string refreshToken, AuthResponseDto result) = await _authService.LoginAsync(dto);
            Response.Cookies.Append("accessToken", token, AccessTokenCookieOptions());
            Response.Cookies.Append("refreshToken", refreshToken, RefreshTokenCookieOptions());
            return Ok(result);
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("refresh")]
        [SwaggerOperation(Summary = "Refresh access token",
            Description = "Issues a new JWT access token using the HttpOnly refresh token cookie.")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Refresh()
        {
            string? refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "No refresh token provided." });
            }

            (string token, string newRefreshToken, AuthResponseDto result) =
                await _authService.RefreshAsync(refreshToken);
            Response.Cookies.Append("accessToken", token, AccessTokenCookieOptions());
            Response.Cookies.Append("refreshToken", newRefreshToken, RefreshTokenCookieOptions());
            return Ok(result);
        }


        [HttpPost("forgot-password")]
        [EnableRateLimiting("forgotpassword")]
        [SwaggerOperation(Summary = "Request password reset",
            Description = "Sends a password reset email if the provided email exists in the system.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            ValidationResult? validation = await _forgotPasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            await _authService.ForgotPasswordAsync(dto.Email, _frontendSettings.ResetPasswordUrl, dto.Lang);
            return Ok(new { message = "If that email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("forgotpassword")]
        [SwaggerOperation(Summary = "Reset password",
            Description = "Resets the user's password using a valid reset token.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            ValidationResult? validation = await _resetPasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

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
            string? refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.RevokeRefreshTokenAsync(refreshToken);
            }

            Response.Cookies.Delete("accessToken", AccessTokenDeleteCookieOptions());
            Response.Cookies.Delete("refreshToken", RefreshTokenDeleteCookieOptions());
            return Ok();
        }

        private CookieOptions AccessTokenCookieOptions()
        {
            return CreateAuthCookieOptions("/", DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));
        }

        private CookieOptions RefreshTokenCookieOptions()
        {
            return CreateAuthCookieOptions("/api/auth/refresh", DateTimeOffset.UtcNow.AddDays(1));
        }

        private CookieOptions RefreshTokenDeleteCookieOptions()
        {
            return CreateAuthCookieOptions("/api/auth/refresh");
        }

        private CookieOptions AccessTokenDeleteCookieOptions()
        {
            return CreateAuthCookieOptions("/");
        }

        private CookieOptions CreateAuthCookieOptions(string path, DateTimeOffset? expires = null)
        {
            bool secure = UseSecureCookies();
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
                Path = path,
                Expires = expires
            };
        }

        private bool UseSecureCookies()
        {
            return !_environment.IsDevelopment() || Request.IsHttps;
        }
    }
}
