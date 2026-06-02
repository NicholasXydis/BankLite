using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }

    [MaxLength(50)] public required string Action { get; init; }

    [MaxLength(500)] public required string Details { get; init; }

    public DateTime PerformedAt { get; init; } = DateTime.UtcNow;
}