namespace Fcg.Payments.Application.Services;

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<GatewayResult> AuthorizeAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default);
    Task<GatewayResult> CaptureAsync(string providerReference, CancellationToken cancellationToken = default);
    Task<GatewayResult> FailAsync(string providerReference, string? reason, CancellationToken cancellationToken = default);
}

public record GatewayResult(bool Success, string? ProviderReference, string? ErrorMessage);
