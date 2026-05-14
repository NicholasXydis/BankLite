using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using BankLite.Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BankLite.API.Controllers
{
    [EnableRateLimiting("fixed")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IValidator<DepositWithdrawDto> _depositWithdrawValidator;
        private readonly IValidator<TransferDto> _transferValidator;
        private readonly IValidator<ExternalTransferDto> _externalTransferValidator;

        public TransactionController(ITransactionService transactionService, IValidator<DepositWithdrawDto> depositwithdrawValidator, IValidator<TransferDto> transferValidator, IValidator<ExternalTransferDto> externalTransferValidator)
        {
            _transactionService = transactionService;
            _depositWithdrawValidator = depositwithdrawValidator;
            _transferValidator = transferValidator;
            _externalTransferValidator = externalTransferValidator;
        }

        [HttpPost("deposit")]
        [ProducesResponseType(typeof(TransactionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Deposit([FromBody] DepositWithdrawDto dto)
        {
            var validation = await _depositWithdrawValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _transactionService.DepositAsync(dto, userId);
            var response = new TransactionResponseDto
            {
                Id = result.Id,
                AccountId = result.AccountId,
                Amount = result.Amount,
                Type = result.Type.ToString(),
                Description = result.Description,
                CreatedAt = result.CreatedAt,
            };

            return Ok(response);
        }

        [HttpPost("withdraw")]
        [ProducesResponseType(typeof(TransactionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Withdraw([FromBody] DepositWithdrawDto dto)
        {
            var validation = await _depositWithdrawValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _transactionService.WithdrawAsync(dto, userId);
            var response = new TransactionResponseDto
            {
                Id = result.Id,
                AccountId = result.AccountId,
                Amount = result.Amount,
                Type = result.Type.ToString(),
                Description = result.Description,
                CreatedAt = result.CreatedAt,
            };
            return Ok(response);
        }

        [HttpPost("transfer")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
        {
            var validation = await _transferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _transactionService.TransferAsync(dto, userId);
            return Ok(new { message = "Transfer successful", amount = dto.Amount });
        }

        [HttpPost("transferexternal")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> TransferExternal([FromBody] ExternalTransferDto dto)
        {

            var validation = await _externalTransferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _transactionService.TransferExternalAsync(dto, userId);
            return Ok(new { message = "Transfer successful", amount = dto.Amount });
        }

        [HttpGet("{accountId}")]
        [ProducesResponseType(typeof(PagedResultDto<TransactionResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetTransactions(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId, page, pageSize);

            var response = new PagedResultDto<TransactionResponseDto>
            {
                Items = result.Items.Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    AccountId = t.AccountId,
                    Amount = t.Amount,
                    Type = t.Type.ToString(),
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
            return Ok(response);
        }

        [HttpGet("{accountId}/range")]
        [ProducesResponseType(typeof(IEnumerable<TransactionResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetTransactionsByDateRange(Guid accountId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _transactionService.GetTransactionsByDateRangeAsync(accountId, userId, startDate, endDate);

            var response = result.Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Description = t.Description,
                CreatedAt = t.CreatedAt
            });

            return Ok(response);
        }
    }
}
