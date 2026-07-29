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
    [Tags("Transactions")]
    public class TransactionController : BaseController
    {
        private readonly IValidator<DepositWithdrawDto> _depositWithdrawValidator;
        private readonly IValidator<ExternalTransferDto> _externalTransferValidator;
        private readonly ITransactionService _transactionService;
        private readonly IValidator<TransferDto> _transferValidator;

        public TransactionController(ITransactionService transactionService,
            IValidator<DepositWithdrawDto> depositwithdrawValidator, IValidator<TransferDto> transferValidator,
            IValidator<ExternalTransferDto> externalTransferValidator)
        {
            _transactionService = transactionService;
            _depositWithdrawValidator = depositwithdrawValidator;
            _transferValidator = transferValidator;
            _externalTransferValidator = externalTransferValidator;
        }

        [HttpPost("deposit")]
        [SwaggerOperation(Summary = "Deposit funds",
            Description =
                "Deposits funds into the specified account. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(typeof(TransactionResponseDto), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationFailure>), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 403)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> Deposit([FromBody] DepositWithdrawDto dto)
        {
            ValidationResult? validation = await _depositWithdrawValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            string? idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            TransactionResponseDto result = await _transactionService.DepositAsync(dto, userId, idempotencyKey);
            return Ok(result);
        }

        [HttpPost("withdraw")]
        [SwaggerOperation(Summary = "Withdraw funds",
            Description =
                "Withdraws funds from the specified account. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(typeof(TransactionResponseDto), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationFailure>), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 403)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> Withdraw([FromBody] DepositWithdrawDto dto)
        {
            ValidationResult? validation = await _depositWithdrawValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            string? idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            TransactionResponseDto result = await _transactionService.WithdrawAsync(dto, userId, idempotencyKey);
            return Ok(result);
        }

        [HttpPost("transfer")]
        [SwaggerOperation(Summary = "Internal transfer",
            Description =
                "Transfers funds between two accounts belonging to the authenticated user. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(typeof(TransferResponseDto), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationFailure>), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 403)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
        {
            ValidationResult? validation = await _transferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            string? idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            await _transactionService.TransferAsync(dto, userId, idempotencyKey);
            return Ok(new TransferResponseDto { Message = "Transfer successful", Amount = dto.Amount });
        }

        [HttpPost("transferexternal")]
        [SwaggerOperation(Summary = "External transfer",
            Description =
                "Transfers funds to another user's account by account number. Supports idempotency via Idempotency-Key header.")]
        [ProducesResponseType(typeof(TransferResponseDto), 200)]
        [ProducesResponseType(typeof(IEnumerable<ValidationFailure>), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 403)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> TransferExternal([FromBody] ExternalTransferDto dto)
        {
            ValidationResult? validation = await _externalTransferValidator.ValidateAsync(dto);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors);
            }

            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            string? idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            await _transactionService.TransferExternalAsync(dto, userId, idempotencyKey);
            return Ok(new TransferResponseDto { Message = "Transfer successful", Amount = dto.Amount });
        }

        [HttpGet("{accountId}")]
        [SwaggerOperation(Summary = "Get transactions",
            Description = "Returns paginated transactions for the specified account. Optionally filter by type.")]
        [ProducesResponseType(typeof(PagedResultDto<TransactionResponseDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> GetTransactions(Guid accountId, [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] string? type = null)
        {
            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            if (pageSize < 1)
            {
                pageSize = 1;
            }

            if (page < 1)
            {
                page = 1;
            }

            PagedResultDto<TransactionResponseDto> result =
                await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId, page, pageSize, type);
            return Ok(result);
        }

        [HttpGet("{accountId}/range")]
        [SwaggerOperation(Summary = "Get transactions by date range",
            Description = "Returns all transactions for the specified account within a date range.")]
        [ProducesResponseType(typeof(IEnumerable<TransactionResponseDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponseDto), 400)]
        [ProducesResponseType(typeof(ErrorResponseDto), 401)]
        [ProducesResponseType(typeof(ErrorResponseDto), 404)]
        [ProducesResponseType(429)]
        [ProducesResponseType(typeof(ErrorResponseDto), 500)]
        public async Task<IActionResult> GetTransactionsByDateRange(Guid accountId, [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            IActionResult? error = TryGetUserId(out Guid userId);
            if (error != null)
            {
                return error;
            }

            if (endDate < startDate)
            {
                return BadRequest(new ErrorResponseDto { Message = "End date must be after start date." });
            }

            if ((endDate - startDate).TotalDays > 365)
            {
                return BadRequest(new ErrorResponseDto { Message = "Date range cannot exceed 365 days." });
            }

            IEnumerable<TransactionResponseDto> result =
                await _transactionService.GetTransactionsByDateRangeAsync(accountId, userId, startDate, endDate);
            return Ok(result);
        }
    }
}