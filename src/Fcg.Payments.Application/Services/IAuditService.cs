using Fcg.Payments.Contracts.Audit;

namespace Fcg.Payments.Application.Services;

public interface IAuditService
{
    Task<IReadOnlyList<AuditEntryResponse>> GetPaymentAuditAsync(Guid paymentId, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default);
}
