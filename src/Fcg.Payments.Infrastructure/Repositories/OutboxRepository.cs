using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly PaymentsDbContext _db;

    public OutboxRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxEvent evt, CancellationToken cancellationToken = default)
    {
        _db.OutboxEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(int take, CancellationToken cancellationToken = default)
    {
        return await _db.OutboxEvents.AsNoTracking()
            .Where(e => e.Status == "Pending")
            .OrderBy(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var evt = await _db.OutboxEvents.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (evt is null) return;
        evt.Status = "Processed";
        evt.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var evt = await _db.OutboxEvents.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (evt is null) return;
        evt.Status = "Failed";
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task IncrementRetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var evt = await _db.OutboxEvents.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (evt is null) return;
        evt.RetryCount++;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
