namespace Fcg.Payments.Contracts.Payments;

public class ConfirmPaymentRequest
{
    public string? IdempotencyKey { get; set; }
}
