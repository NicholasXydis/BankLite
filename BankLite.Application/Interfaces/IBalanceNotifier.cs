namespace BankLite.Application.Interfaces;

public interface IBalanceNotifier
{
    Task NotifyBalanceUpdatedAsync(Guid userId, Guid accountId, decimal newBalance);
}