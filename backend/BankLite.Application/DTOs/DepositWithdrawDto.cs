namespace BankLite.Application.DTOs;

public class DepositWithdrawDto
{
    /// <summary>The unique identifier of the account to deposit into or withdraw from.</summary>
    public Guid AccountId { get; init; }

    /// <summary>The amount to deposit or withdraw. Must be greater than 0 and no more than 1,000,000.</summary>
    public decimal Amount { get; init; }
}
