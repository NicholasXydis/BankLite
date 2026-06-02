using BankLite.Domain.Entities;

namespace BankLite.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task<Account?> GetByAccountNumberAsync(string accountNumber);
    Task<bool> ExistsByAccountNumberAsync(string accountNumber);
    Task<bool> ExistsByUserIdAndTypeAsync(Guid userId, AccountType accountType);
}