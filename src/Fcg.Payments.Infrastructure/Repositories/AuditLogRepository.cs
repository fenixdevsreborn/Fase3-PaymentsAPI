using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PaymentsDbContext _db;

    public AuditLogRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditLog>> GetByAggregateAsync(string aggregateType, string aggregateId, CancellationToken cancellationToken = default)
    {
        return await _db.AuditLogs.AsNoTracking()
            .Where(a => a.AggregateType == aggregateType && a.AggregateId == aggregateId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
