using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;

    [MaxLength(256)] public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsUsed { get; set; }
}