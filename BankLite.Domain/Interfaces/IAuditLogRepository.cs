using BankLite.Domain.Entities;

namespace BankLite.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task LogAsync(AuditLog auditlog);
}