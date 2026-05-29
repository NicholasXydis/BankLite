using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities;

public enum AccountType
{
    Chequing,
    Savings
}

public class Account
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;

    [MaxLength(20)] public required string AccountNumber { get; init; }

    public AccountType Type { get; init; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<Transaction> Transactions { get; init; } = [];
}