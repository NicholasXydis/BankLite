using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using BankLite.Infrastructure.Data;

namespace BankLite.Infrastructure.Repositories;

public class AuditLogRepository(BankLiteDbContext context) : IAuditLogRepository
{
    public async Task LogAsync(AuditLog auditlog)
    {
        await context.AuditLogs.AddAsync(auditlog);
    }
}
