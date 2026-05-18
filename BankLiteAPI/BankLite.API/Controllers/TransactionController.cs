using Azure;
using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BankLite.API.Controllers
{
    [EnableRateLimiting("fixed")]
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Transactions")]
    public class TransactionController : BaseController
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
        [SwaggerOperation(Summary = "Deposit funds", Description = "Deposits funds into the specified account. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(typeof(TransactionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Deposit([FromBody] DepositWithdrawDto dto)
        {
            var validation = await _depositWithdrawValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var error = TryGetUserId(out var userId);
            if (error != null) return error;
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            var result = await _transactionService.DepositAsync(dto, userId, idempotencyKey);
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
        [SwaggerOperation(Summary = "Withdraw funds", Description = "Withdraws funds from the specified account. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(typeof(TransactionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Withdraw([FromBody] DepositWithdrawDto dto)
        {
            var validation = await _depositWithdrawValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var error = TryGetUserId(out var userId);
            if (error != null) return error;
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            var result = await _transactionService.WithdrawAsync(dto, userId, idempotencyKey);
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
        [SwaggerOperation(Summary = "Internal transfer", Description = "Transfers funds between two accounts belonging to the authenticated user. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
        {
            var validation = await _transferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var error = TryGetUserId(out var userId);
            if (error != null) return error;
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            await _transactionService.TransferAsync(dto, userId, idempotencyKey);
            return Ok(new { message = "Transfer successful", amount = dto.Amount });
        }

        [HttpPost("transferexternal")]
        [SwaggerOperation(Summary = "External transfer", Description = "Transfers funds to another user's account by account number. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> TransferExternal([FromBody] ExternalTransferDto dto)
        {

            var validation = await _externalTransferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var error = TryGetUserId(out var userId);
            if (error != null) return error;
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            await _transactionService.TransferExternalAsync(dto, userId, idempotencyKey);
            return Ok(new { message = "Transfer successful", amount = dto.Amount });
        }

        [HttpGet("{accountId}")]
        [SwaggerOperation(Summary = "Get transactions", Description = "Returns paginated transactions for the specified account. Optionally filter by type.")]
        [ProducesResponseType(typeof(PagedResultDto<TransactionResponseDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTransactions(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? type = null)
        {
            var error = TryGetUserId(out var userId);
            if (error != null) return error;

            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            var result = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId, page, pageSize, type);

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
        [SwaggerOperation(Summary = "Get transactions by date range", Description = "Returns all transactions for the specified account within a date range.")]
        [ProducesResponseType(typeof(IEnumerable<TransactionResponseDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTransactionsByDateRange(Guid accountId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var error = TryGetUserId(out var userId);
            if (error != null) return error;

            if (endDate < startDate) return BadRequest(new { message = "End date must be after start date." });
            if ((endDate - startDate).TotalDays > 365) return BadRequest(new { message = "Date range cannot exceed 365 days." });

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
