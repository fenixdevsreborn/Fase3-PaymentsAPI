namespace Fcg.Payments.Contracts.Payments;

public class WebhookProviderRequest
{
    public string? ProviderReference { get; set; }
    public string? Status { get; set; }
    public string? Signature { get; set; }
}
