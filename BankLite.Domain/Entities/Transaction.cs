using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer
}

public class Transaction
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public Account Account { get; init; } = null!;
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }

    [MaxLength(500)] public string Description { get; init; } = string.Empty;

    [MaxLength(64)] public string? IdempotencyKey { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}