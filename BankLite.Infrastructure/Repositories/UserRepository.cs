using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using BankLite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankLite.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly BankLiteDbContext _context;

        public UserRepository(BankLiteDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsAsync(string email)
        {
            return await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
        }
    }
}
