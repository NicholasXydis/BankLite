using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities
{
    public enum TransactionType { Deposit, Withdrawal, Transfer }

    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Account Account { get; set; } = null!;
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? IdempotencyKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
