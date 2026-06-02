using System.Data;

namespace BankLite.Domain.Interfaces;

public interface IUnitOfWork
{
    Task SaveAsync();
    Task ExecuteInTransactionAsync(Func<Task> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel);
}