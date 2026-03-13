using Fcg.Payments.Domain.Entities;
using Fcg.Payments.Domain.Enums;
using Fcg.Payments.Domain.Repositories;
using Fcg.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payments.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _db;

    public PaymentRepository(PaymentsDbContext db)
    {
        _db = db;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        return await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.ProviderReference == providerReference, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Payment?> GetPendingByUserAndGameAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        return await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.GameId == gameId && p.Status == PaymentStatus.Pending, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _db.Payments.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Payments.CountAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Payments.FindAsync(new object[] { payment.Id }, cancellationToken).ConfigureAwait(false);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(payment);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
