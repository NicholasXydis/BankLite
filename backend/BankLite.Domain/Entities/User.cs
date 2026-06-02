using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities;

public class User
{
    public Guid Id { get; init; }

    [MaxLength(50)] public required string FullName { get; init; }

    [MaxLength(256)] public required string Email { get; init; }

    [MaxLength(256)] public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public ICollection<Account> Accounts { get; init; } = new List<Account>();
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; init; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; init; } = new List<PasswordResetToken>();
}