using System.Data;
using BankLite.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankLite.Infrastructure.Data;

public class UnitOfWork(BankLiteDbContext context) : IUnitOfWork
{
    public async Task SaveAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        await ExecuteInTransactionAsync(operation, IsolationLevel.Serializable);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
            try
            {
                await operation();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                throw;
            }
        }).ConfigureAwait(false);
    }
}
