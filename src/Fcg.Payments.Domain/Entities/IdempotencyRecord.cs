namespace Fcg.Payments.Domain.Entities;

public class IdempotencyRecord
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
