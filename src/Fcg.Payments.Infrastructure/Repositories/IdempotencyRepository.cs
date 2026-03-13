using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly PaymentsDbContext _db;

    public IdempotencyRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetPaymentIdAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var r = await _db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken).ConfigureAwait(false);
        return r?.PaymentId;
    }

    public async Task SetAsync(string idempotencyKey, string paymentId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.IdempotencyRecords.FindAsync(new object[] { idempotencyKey }, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return;
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            PaymentId = paymentId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
