using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BankLite.API.Controllers
{
    [EnableRateLimiting("fixed")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IValidator<CreateAccountDto> _accountValidator;

        public AccountController(IAccountService accountService, IValidator<CreateAccountDto> accountValidator)
        {
            _accountService = accountService;
            _accountValidator = accountValidator;
        }


        [HttpPost("create")]
        [ProducesResponseType(typeof(AccountResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
        {
            var validation = await _accountValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var result = await _accountService.CreateAccountAsync(dto, userId);

                var response = new AccountResponseDto
                {
                    Id = result.Id,
                    AccountNumber = result.AccountNumber,
                    Type = result.Type.ToString(),
                    Balance = result.Balance,
                    CreatedAt = result.CreatedAt
                };
                return StatusCode(201, response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AccountResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAccounts()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _accountService.GetAccountsByUserIdAsync(userId);

            var response = result.Select(a => new AccountResponseDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                Type = a.Type.ToString(),
                Balance = a.Balance,
                CreatedAt = a.CreatedAt
            });
            return Ok(response);
        }
    }
}
