using Fcg.Payments.Application.Services;

namespace Fcg.Payments.Infrastructure.Gateways;

public class FakePaymentGateway : IPaymentGateway
{
    public string ProviderName => "Fake";

    public Task<GatewayResult> AuthorizeAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default)
    {
        var reference = $"fake-{paymentId:N}";
        return Task.FromResult(new GatewayResult(true, reference, null));
    }

    public Task<GatewayResult> CaptureAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayResult(true, providerReference, null));
    }

    public Task<GatewayResult> FailAsync(string providerReference, string? reason, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayResult(true, providerReference, null));
    }
}
