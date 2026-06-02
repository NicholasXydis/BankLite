using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using BankLite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankLite.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly BankLiteDbContext _context;

    public AccountRepository(BankLiteDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Accounts.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<bool> ExistsByAccountNumberAsync(string accountNumber)
    {
        return await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<bool> ExistsByUserIdAndTypeAsync(Guid userId, AccountType accountType)
    {
        return await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.UserId == userId && a.Type == accountType);
    }
}
