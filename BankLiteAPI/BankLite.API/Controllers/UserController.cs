using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("User")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;

        public UserController(IUserService userService, IValidator<ChangePasswordDto> changePasswordValidator)
        {
            _userService = userService;
            _changePasswordValidator = changePasswordValidator;
        }

        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get profile", Description = "Returns the authenticated user's profile information.")]
        [ProducesResponseType(typeof(UserProfileDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [HttpPost("change-password")]
        [SwaggerOperation(Summary = "Change password", Description = "Changes the authenticated user's password.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var validation = await _changePasswordValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _userService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpDelete("delete-account")]
        [SwaggerOperation(Summary = "Delete account", Description = "Permanently deletes the authenticated user's account and all associated data.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _userService.DeleteAccountAsync(userId);
            return Ok(new { message = "Account deleted successfully" });
        }
    }
}