using BankLite.Application.DTOs;

namespace BankLite.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionResponseDto> DepositAsync(DepositWithdrawDto dto, Guid userId, string? idempotencyKey = null);
    Task<TransactionResponseDto> WithdrawAsync(DepositWithdrawDto dto, Guid userId, string? idempotencyKey = null);
    Task TransferAsync(TransferDto dto, Guid userId, string? idempotencyKey = null);
    Task TransferExternalAsync(ExternalTransferDto dto, Guid userId, string? idempotencyKey = null);

    Task<PagedResultDto<TransactionResponseDto>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId, int page,
        int pageSize, string? type = null);

    Task<IEnumerable<TransactionResponseDto>> GetTransactionsByDateRangeAsync(Guid accountId, Guid userId,
        DateTime startDate, DateTime endDate);
}