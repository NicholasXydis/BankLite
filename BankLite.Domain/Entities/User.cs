using System;
using System.Collections.Generic;

namespace BankLite.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
