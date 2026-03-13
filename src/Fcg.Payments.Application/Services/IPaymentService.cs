using Fcg.Payments.Contracts.Paging;
using Fcg.Payments.Contracts.Payments;

namespace Fcg.Payments.Application.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> GetByIdAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<PagedResponse<PaymentResponse>> GetMyPaymentsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> ConfirmAsync(Guid paymentId, Guid? userId, bool isAdmin, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<PaymentResponse?> FailAsync(Guid paymentId, Guid? userId, bool isAdmin, string? failureReason, string? idempotencyKey, CancellationToken cancellationToken = default);
    Task<bool> ProcessWebhookAsync(WebhookProviderRequest request, CancellationToken cancellationToken = default);
}
