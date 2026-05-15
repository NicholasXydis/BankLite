using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        [MaxLength(50)]
        public required string FullName { get; set; }

        [MaxLength(256)]
        public required string Email { get; set; }

        [MaxLength(256)]
        public required string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
