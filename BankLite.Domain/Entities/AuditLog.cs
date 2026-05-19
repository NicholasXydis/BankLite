using System.ComponentModel.DataAnnotations;

namespace BankLite.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        [MaxLength(50)]
        public required string Action { get; set; }

        [MaxLength(500)]
        public required string Details { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }
}
