namespace Fcg.Payments.Domain.Repositories;

public interface IIdempotencyRepository
{
    Task<string?> GetPaymentIdAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetAsync(string idempotencyKey, string paymentId, CancellationToken cancellationToken = default);
}
