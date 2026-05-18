using BankLite.Domain.Entities;
using System;
using System.Collections.Generic;

namespace BankLite.Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid Id);
        Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId);
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
        Task<Account?> GetByAccountNumberAsync(string accountNumber);
        Task<bool> ExistsByAccountNumberAsync(string accountNumber);
    }
}
