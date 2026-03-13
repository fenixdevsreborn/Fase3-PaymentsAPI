using Fcg.Payments.Contracts.Audit;
using Fcg.Payments.Domain.Repositories;

namespace Fcg.Payments.Application.Services;

public class AuditService : IAuditService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IPaymentRepository paymentRepository, IAuditLogRepository auditLogRepository)
    {
        _paymentRepository = paymentRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<IReadOnlyList<AuditEntryResponse>> GetPaymentAuditAsync(Guid paymentId, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null) return Array.Empty<AuditEntryResponse>();
        if (!isAdmin && payment.UserId != userId)
            return Array.Empty<AuditEntryResponse>();

        var entries = await _auditLogRepository.GetByAggregateAsync("Payment", paymentId.ToString(), cancellationToken).ConfigureAwait(false);
        return entries.Select(a => new AuditEntryResponse
        {
            Id = a.Id,
            AggregateType = a.AggregateType,
            AggregateId = a.AggregateId,
            Action = a.Action,
            OldData = a.OldData,
            NewData = a.NewData,
            TraceId = a.TraceId,
            CorrelationId = a.CorrelationId,
            CreatedAt = a.CreatedAt
        }).ToList();
    }
}
