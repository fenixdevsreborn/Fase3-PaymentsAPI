using Fcg.Payments.Domain.Entities;

namespace Fcg.Payments.Domain.Repositories;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetByAggregateAsync(string aggregateType, string aggregateId, CancellationToken cancellationToken = default);
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
