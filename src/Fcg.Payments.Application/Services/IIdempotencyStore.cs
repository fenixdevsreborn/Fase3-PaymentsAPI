namespace Fcg.Payments.Application.Services;

/// <summary>Abstraction for idempotency; implementation can use Domain's IIdempotencyRepository.</summary>
public interface IIdempotencyStore
{
    Task<string?> GetResultAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetResultAsync(string idempotencyKey, string paymentId, CancellationToken cancellationToken = default);
}
