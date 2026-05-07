using BankLite.Domain.Entities;
using System;

namespace BankLite.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<bool> ExistsAsync(string email);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
    }
}
