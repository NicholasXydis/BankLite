using BankLite.Domain.Entities;
using BankLite.Domain.Interfaces;
using BankLite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankLite.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly BankLiteDbContext _context;

        public AuditLogRepository(BankLiteDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId)
        {
            return await _context.AuditLogs.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();
        }

        public async Task LogAsync(AuditLog auditlog)
        {
            await _context.AuditLogs.AddAsync(auditlog);
        }
    }
}
