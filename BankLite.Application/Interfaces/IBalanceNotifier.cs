namespace BankLite.Application.Interfaces;

public interface IBalanceNotifier
{
    Task NotifyBalanceUpdatedAsync(string userId, Guid accountId, decimal newBalance);
}