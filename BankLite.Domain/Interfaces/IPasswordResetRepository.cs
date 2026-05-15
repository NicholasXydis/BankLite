using BankLite.Domain.Entities;

namespace BankLite.Domain.Interfaces
{
    public interface IPasswordResetRepository
    {
        Task AddAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetByTokenAsync(string token);
        Task UpdateAsync(PasswordResetToken token);
    }
}