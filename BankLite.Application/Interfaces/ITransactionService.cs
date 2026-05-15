using BankLite.Application.DTOs;
using BankLite.Domain.Entities;

namespace BankLite.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction> DepositAsync(DepositWithdrawDto dto, Guid userId, string? idempotencyKey = null);
        Task<Transaction> WithdrawAsync(DepositWithdrawDto dto, Guid userId, string? idempotencyKey = null);
        Task TransferAsync(TransferDto dto, Guid userId, string? idempotencyKey = null);
        Task TransferExternalAsync(ExternalTransferDto dto, Guid userId, string? idempotencyKey = null);
        Task<PagedResultDto<Transaction>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId, int page, int pageSize, string? type = null);
        Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(Guid accountId, Guid userId, DateTime startDate, DateTime endDate);
    }
}
