using BankLite.Domain.Entities;

namespace BankLite.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, int page, int pageSize, string? type = null);
        Task<int> GetTotalCountAsync(Guid accountId, string? type = null);
        Task AddAsync(Transaction transaction);
        Task<IEnumerable<Transaction>> GetByAccountIdAndDateRangeAsync(Guid accountId, DateTime startDate, DateTime endDate);
    }
}
