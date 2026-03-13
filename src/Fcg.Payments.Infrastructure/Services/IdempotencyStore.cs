using Fcg.Payments.Application.Services;
using Fcg.Payments.Domain.Repositories;

namespace Fcg.Payments.Infrastructure.Services;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly IIdempotencyRepository _repo;

    public IdempotencyStore(IIdempotencyRepository repo)
    {
        _repo = repo;
    }

    public Task<string?> GetResultAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => _repo.GetPaymentIdAsync(idempotencyKey, cancellationToken);

    public Task SetResultAsync(string idempotencyKey, string paymentId, CancellationToken cancellationToken = default)
        => _repo.SetAsync(idempotencyKey, paymentId, cancellationToken);
}
