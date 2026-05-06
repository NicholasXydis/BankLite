using BankLite.Application.DTOs;
using BankLite.Domain.Entities;

namespace BankLite.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction> DepositAsync(DepositWithdrawDto dto, Guid userId);
        Task<Transaction> WithdrawAsync(DepositWithdrawDto dto, Guid userId);
        Task TransferAsync(TransferDto dto, Guid userId);
        Task TransferExternalAsync(ExternalTransferDto dto, Guid userId);
        Task<PagedResultDto<Transaction>> GetTransactionsByAccountIdAsync(Guid accountId, Guid userId, int page, int pageSize);
        Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(Guid accountId, Guid userId, DateTime startDate, DateTime endDate);
    }
}
