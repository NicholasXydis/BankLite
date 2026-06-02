namespace BankLite.Application.DTOs;

public class TransactionResponseDto
{
    /// <summary>The unique transaction identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>The account this transaction belongs to.</summary>
    public Guid AccountId { get; init; }

    /// <summary>The transaction amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>The transaction type: Deposit, Withdrawal, or Transfer.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>A description of the transaction.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The date and time the transaction was created.</summary>
    public DateTime CreatedAt { get; init; }
}