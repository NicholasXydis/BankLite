namespace BankLite.Application.DTOs;

public class ExternalTransferDto
{
    /// <summary>The unique identifier of the account to transfer from.</summary>
    public Guid FromAccountId { get; init; }

    /// <summary>The 12-character account number to transfer to.</summary>
    public string ToAccountNumber { get; init; } = string.Empty;

    /// <summary>The amount to transfer. Must be greater than 0 and no more than 1,000,000.</summary>
    public decimal Amount { get; init; }
}
