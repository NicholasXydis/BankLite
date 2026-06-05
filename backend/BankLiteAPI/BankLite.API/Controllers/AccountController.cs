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
    [EnableRateLimiting("fixed")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Accounts")]
    public class AccountController : BaseController
    {
        private readonly IAccountService _accountService;
        private readonly IValidator<CreateAccountDto> _accountValidator;

        public AccountController(IAccountService accountService, IValidator<CreateAccountDto> accountValidator)
        {
            _accountService = accountService;
            _accountValidator = accountValidator;
        }


        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create account",
            Description = "Creates a new chequing or savings account for the authenticated user.")]
        [ProducesResponseType(typeof(AccountResponseDto), 201)]
        [ProducesResponseType(typeof(IEnumerable<ValidationFailure>), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 403)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
        {
            ValidationResult? validation = await _accountValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            AccountResponseDto result = await _accountService.CreateAccountAsync(dto, userId);
            return StatusCode(201, result);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get accounts",
            Description = "Returns all accounts belonging to the authenticated user.")]
        [ProducesResponseType(typeof(IEnumerable<AccountResponseDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> GetAccounts()
        {
            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            IEnumerable<AccountResponseDto> result = await _accountService.GetAccountsByUserIdAsync(userId);
            return Ok(result);
        }
    }
}
