using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities
{
    public enum AccountType { Chequing, Savings }

    public class Account
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(20)]
        public required string AccountNumber { get; set; }

        public AccountType Type { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Transaction> Transactions { get; set; } = new();
    }
}
