namespace BankLite.Application.DTOs;

public class AccountResponseDto
{
    /// <summary>The unique account identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>The 12-character unique account number.</summary>
    public string AccountNumber { get; init; } = string.Empty;

    /// <summary>The account type: Chequing or Savings.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>The current account balance.</summary>
    public decimal Balance { get; init; }

    /// <summary>The date the account was created.</summary>
    public DateTime CreatedAt { get; init; }
}