using Fcg.Payments.Domain.Entities;

namespace Fcg.Payments.Domain.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent evt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(int take, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid id, CancellationToken cancellationToken = default);
    Task IncrementRetryAsync(Guid id, CancellationToken cancellationToken = default);
}
