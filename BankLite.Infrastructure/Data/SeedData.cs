using BankLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankLite.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(BankLiteDbContext context)
        {
            if (await context.Users.AnyAsync()) return;

            var user = new User
            {
                FullName = "Test User",
                Email = "test@banklite.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            };

            await context.Users.AddAsync(user);

            var chequing = new Account
            {
                UserId = user.Id,
                Type = AccountType.Chequing,
                AccountNumber = "TESTCHQ00001",
                Balance = 1000m
            };

            var savings = new Account
            {
                UserId = user.Id,
                Type = AccountType.Savings,
                AccountNumber = "TESTSAV00001",
                Balance = 5000m
            };

            await context.Accounts.AddRangeAsync(chequing, savings);
            await context.SaveChangesAsync();
        }
    }
}
